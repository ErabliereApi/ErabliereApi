using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Donnees.Action.Post;
using ErabliereApi.Donnees.Contantes;
using ErabliereApi.Extensions;
using ErabliereApi.Services.AI.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using System.ClientModel;
using System.Security.Cryptography;
using System.Text;

namespace ErabliereApi.Services.AI;

/// <summary>
/// Default <see cref="IConversationAIService" /> implementation.
/// </summary>
public class ConversationAIService : IConversationAIService
{
    private const string ChatPromptType = "Chat";

    /// <summary>
    /// Told to the model on the turn that follows a limit, when the tools are taken
    /// away. Without it, a model that was mid-investigation tends to answer that it
    /// needs one more call, which the user can do nothing about.
    /// </summary>
    public const string LimitReachedInstruction =
        "Vous ne pouvez plus consulter d'outil pour cette question. " +
        "Répondez maintenant avec les données déjà obtenues, en précisant clairement ce que vous n'avez pas pu vérifier.";

    private readonly ErabliereDbContext _depot;
    private readonly IConfiguration _configuration;
    private readonly IAIService _aiService;
    private readonly ISystemPromptBuilder _systemPromptBuilder;
    private readonly IErabliereAiToolset _toolset;
    private readonly IErabliereAiCapteurCatalog _capteurCatalog;
    private readonly IErabliereAiCapabilityService _capabilityService;
    private readonly IToolActivityTracker _activityTracker;
    private readonly IOptions<ErabliereAiToolOptions> _toolOptions;
    private readonly ILogger<ConversationAIService> _logger;

    /// <summary>
    /// Constructeur par initialisation
    /// </summary>
    public ConversationAIService(
        ErabliereDbContext depot,
        IConfiguration configuration,
        IAIService aiService,
        ISystemPromptBuilder systemPromptBuilder,
        IErabliereAiToolset toolset,
        IErabliereAiCapteurCatalog capteurCatalog,
        IErabliereAiCapabilityService capabilityService,
        IToolActivityTracker activityTracker,
        IOptions<ErabliereAiToolOptions> toolOptions,
        ILogger<ConversationAIService> logger)
    {
        _depot = depot;
        _configuration = configuration;
        _aiService = aiService;
        _systemPromptBuilder = systemPromptBuilder;
        _toolset = toolset;
        _capteurCatalog = capteurCatalog;
        _capabilityService = capabilityService;
        _activityTracker = activityTracker;
        _toolOptions = toolOptions;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PostPromptResponse> SendPromptAsync(PostPrompt prompt, string? userId, CancellationToken token)
    {
        var conversation = await GetOrCreateConversationAsync(prompt, userId, token);

        try
        {
            var exchange = prompt.PromptType == ChatPromptType ?
                await CompleteChatAsync(prompt, conversation, token) :
                new AiExchange(await CompleteSinglePromptAsync(prompt, conversation, token), []);

            var response = await PersistExchangeAsync(prompt, exchange, token);

            return new PostPromptResponse
            {
                Prompt = prompt,
                Conversation = conversation,
                Response = response,
            };
        }
        finally
        {
            // The client polls until this flips, whatever the outcome.
            _activityTracker.Complete(prompt.ActivityId);
        }
    }

    /// <summary>
    /// Complete a prompt using the whole history of the conversation, calling the
    /// read-only tools when the caller's plan opens them.
    /// </summary>
    private async Task<AiExchange> CompleteChatAsync(PostPrompt prompt, Conversation conversation, CancellationToken token)
    {
        var tools = await ResolveToolsAsync(token);

        // Read as the caller, before the model is asked anything: it is one call, it
        // saves the round the model would have spent listing them, and it removes the
        // guess that a sensor whose name shares no word with the question is missing.
        var capteurs = tools.Count > 0 && prompt.ErabliereId is { } erabliereId ?
            await _capteurCatalog.ReadAsync(erabliereId, token) :
            [];

        var messagesPrompt = new List<ChatMessage>
        {
            new SystemChatMessage(_systemPromptBuilder.BuildForCompletion(
                conversation.SystemMessage,
                new ErabliereAiPromptContext(tools.Count > 0, prompt.ErabliereId, prompt.ErabliereNom, capteurs)))
        };

        var messages = await _depot.Messages
            .Where(m => m.ConversationId == prompt.ConversationId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(token);

        foreach (var message in messages)
        {
            // The tool calls and results of the previous prompts are persisted for
            // the user interface, not replayed here: the answer that followed them
            // already carries what they said, and an assistant message whose tool
            // calls are not immediately followed by their results is rejected by the
            // chat completion apis.
            if (TypesMessage.EstMessageOutil(message.MessageType))
            {
                continue;
            }

            messagesPrompt.Add(message.IsUser ?
                new UserChatMessage(message.Content) :
                new AssistantChatMessage(message.Content));
        }

        messagesPrompt.Add(BuildUserMessage(prompt));

        try
        {
            if (tools.Count == 0)
            {
                var answer = await _aiService.CompleteChatAsync(messagesPrompt, BuildCompletionOptions(conversation), token);

                return new AiExchange(answer, []);
            }

            return await RunToolLoopAsync(prompt, conversation, messagesPrompt, tools, token);
        }
        catch (ClientResultException e)
        {
            throw new AIChatCompletionException(e);
        }
    }

    /// <summary>
    /// The tool calling loop: ask the model, run what it asked for, hand the results
    /// back, and start over until it answers or a limit is reached.
    /// </summary>
    /// <remarks>
    /// Three bounds close the loop, and all three end the same way — one last
    /// completion with the tools removed — so the user always gets the best answer
    /// that could be built rather than an error:
    /// <list type="bullet">
    ///   <item>at most <c>MaxRounds</c> turns in which tools are offered;</item>
    ///   <item>a per tool timeout, enforced by the tool set itself;</item>
    ///   <item>a budget on what the tool results are allowed to cost in context.</item>
    /// </list>
    /// </remarks>
    private async Task<AiExchange> RunToolLoopAsync(
        PostPrompt prompt,
        Conversation conversation,
        List<ChatMessage> messagesPrompt,
        IReadOnlyList<ChatTool> tools,
        CancellationToken token)
    {
        var options = _toolOptions.Value;
        var results = new List<ErabliereAiToolResult>();
        var spentTokens = 0;

        for (var round = 1; round <= options.MaxRounds; round++)
        {
            _activityTracker.Publish(prompt.ActivityId, new ToolActivityStep(round, null, ToolActivityLabels.Thinking));

            var response = await _aiService.CompleteChatAsync(
                messagesPrompt,
                BuildCompletionOptions(conversation, tools, toolExchange: true),
                token);

            if (response is null || response.ToolCalls.Count == 0)
            {
                return new AiExchange(response, results);
            }

            messagesPrompt.Add(BuildAssistantToolCallMessage(response.ToolCalls));

            foreach (var toolCall in response.ToolCalls)
            {
                _activityTracker.Publish(
                    prompt.ActivityId,
                    new ToolActivityStep(round, toolCall.FunctionName, ToolActivityLabels.For(toolCall.FunctionName)));

                var result = await _toolset.InvokeAsync(toolCall, token);

                results.Add(result);
                spentTokens += result.EstimatedTokens;

                messagesPrompt.Add(new ToolChatMessage(toolCall.Id, result.ResultJson));
            }

            if (spentTokens >= options.TokenBudget)
            {
                _logger.LogInformation(
                    "ErabliereAI stopped calling tools after {Rounds} rounds: the results reached the budget of {Budget} tokens.",
                    round, options.TokenBudget);

                break;
            }
        }

        return new AiExchange(await CompleteWithoutToolsAsync(conversation, messagesPrompt, token), results);
    }

    /// <summary>
    /// The turn that closes a loop stopped by a limit. The tools are not declared, so
    /// the model has no choice but to answer with what it gathered.
    /// </summary>
    private async Task<AIResponse?> CompleteWithoutToolsAsync(
        Conversation conversation,
        List<ChatMessage> messagesPrompt,
        CancellationToken token)
    {
        messagesPrompt.Add(new SystemChatMessage(LimitReachedInstruction));

        // Still the temperature of a tool exchange: this is the turn that writes the
        // numbers that were read into a sentence, which is exactly where an invented
        // one would do the most damage.
        return await _aiService.CompleteChatAsync(
            messagesPrompt,
            BuildCompletionOptions(conversation, toolExchange: true),
            token);
    }

    /// <summary>
    /// The tools this prompt may use: none unless the feature is on, the provider can
    /// be driven through a tool loop, and the caller's plan grants the capability.
    /// </summary>
    private async Task<IReadOnlyList<ChatTool>> ResolveToolsAsync(CancellationToken token)
    {
        if (!_aiService.SupportsToolCalling)
        {
            return [];
        }

        var capabilities = await _capabilityService.GetCapabilitiesAsync(token);

        return capabilities.ToolsEnabled ? _toolset.GetChatTools() : [];
    }

    private static AssistantChatMessage BuildAssistantToolCallMessage(IReadOnlyList<AIToolCall> toolCalls)
    {
        return new AssistantChatMessage(toolCalls.Select(toolCall => ChatToolCall.CreateFunctionToolCall(
            toolCall.Id,
            toolCall.FunctionName,
            BinaryData.FromString(string.IsNullOrWhiteSpace(toolCall.FunctionArguments) ? "{}" : toolCall.FunctionArguments))));
    }

    /// <summary>
    /// Complete a prompt without any conversation history.
    /// </summary>
    private Task<AIResponse?> CompleteSinglePromptAsync(PostPrompt prompt, Conversation conversation, CancellationToken token)
    {
        return _aiService.CompleteChatAsync([prompt.Prompt], BuildCompletionOptions(conversation), token);
    }

    /// <summary>
    /// The options of one completion.
    /// </summary>
    /// <param name="conversation">The conversation being answered.</param>
    /// <param name="tools">The tools to declare, none when the model must answer.</param>
    /// <param name="toolExchange">
    /// True on every completion of a tool driven exchange, the rounds that declare
    /// tools and the one that closes them alike. It lowers the temperature, because
    /// reading data and writing what was read both want the model to stick to what is
    /// in front of it rather than to be inventive.
    /// </param>
    private ChatCompletionOptions BuildCompletionOptions(
        Conversation conversation,
        IReadOnlyList<ChatTool>? tools = null,
        bool toolExchange = false)
    {
        var options = new ChatCompletionOptions
        {
            Temperature = ResolveTemperature(toolExchange),
            FrequencyPenalty = 0,
            PresencePenalty = 0,
            EndUserId = MD5Hash(conversation.UserId)
        };

        if (tools != null)
        {
            foreach (var tool in tools)
            {
                options.Tools.Add(tool);
            }
        }

        return options;
    }

    /// <summary>
    /// The temperature of a completion: the one of the tool options during a tool
    /// exchange, the one of the platform otherwise.
    /// </summary>
    private float ResolveTemperature(bool toolExchange)
    {
        var temperature = _configuration.GetRequiredValue<float>("LLMDefaultTemperature");

        return toolExchange ? _toolOptions.Value.Temperature ?? temperature : temperature;
    }

    /// <summary>
    /// Persist the question, the tool calls it triggered and the answer, and return
    /// the persisted answer.
    /// </summary>
    private async Task<Message> PersistExchangeAsync(PostPrompt prompt, AiExchange exchange, CancellationToken token)
    {
        // One timestamp per message, one millisecond apart: the conversation is read
        // back ordered by CreatedAt, and a tie would let a result be shown before the
        // call that produced it.
        var createdAt = DateTimeOffset.Now;
        var offset = 0;

        var query = new Message
        {
            ConversationId = prompt.ConversationId,
            Content = prompt.Prompt ?? "",
            IsUser = true,
            CreatedAt = createdAt,
            MessageType = TypesMessage.Texte,
            MessageParts = GetMessagesParts(prompt.Attachments)
        };

        await _depot.Messages.AddAsync(query, token);

        foreach (var result in exchange.ToolResults)
        {
            await _depot.Messages.AddAsync(new Message
            {
                ConversationId = prompt.ConversationId,
                Content = result.ArgumentsJson,
                IsUser = false,
                CreatedAt = createdAt.AddMilliseconds(++offset),
                MessageType = TypesMessage.AppelOutil,
                ToolName = result.ToolName,
                ToolCallId = result.ToolCallId
            }, token);

            await _depot.Messages.AddAsync(new Message
            {
                ConversationId = prompt.ConversationId,
                Content = result.ResultJson,
                IsUser = false,
                CreatedAt = createdAt.AddMilliseconds(++offset),
                MessageType = TypesMessage.ResultatOutil,
                ToolName = result.ToolName,
                ToolCallId = result.ToolCallId
            }, token);
        }

        var aiResponse = exchange.Answer;

        var response = new Message
        {
            ConversationId = prompt.ConversationId,
            Content = aiResponse?.Text ?? "Aucune réponse",
            IsUser = false,
            CreatedAt = createdAt.AddMilliseconds(++offset),
            MessageType = TypesMessage.Texte,
            Refusal = aiResponse?.Refusal,
            // Only a tool that actually returned data makes an answer live: a round of
            // failed calls would otherwise decorate a plain answer with a badge saying
            // it was built on real readings.
            UsedLiveData = exchange.ToolResults.Any(result => !result.IsError),
            ImageUri = aiResponse?.Kind == ChatMessageContentPartKind.Image.ToString() ? aiResponse?.ImageUri?.ToString() : null
        };

        await _depot.Messages.AddAsync(response, token);
        await _depot.SaveChangesAsync(token);

        return response;
    }

    private async Task<Conversation> GetOrCreateConversationAsync(PostPrompt prompt, string? userId, CancellationToken token)
    {
        Conversation? conversation = null;

        if (prompt.ConversationId != null)
        {
            conversation = await _depot.Conversations.FindAsync([prompt.ConversationId], token);

            if (conversation != null)
            {
                conversation.LastMessageDate = DateTime.Now;
            }
        }

        if (conversation == null)
        {
            conversation = new Conversation
            {
                Id = prompt.ConversationId,
                UserId = userId,
                CreatedOn = DateTime.Now,
                LastMessageDate = DateTime.Now,
                Name = prompt.Prompt,
                SystemMessage = _systemPromptBuilder.BuildForNewConversation(prompt.SystemMessage),
            };
            _depot.Conversations.Add(conversation);
            await _depot.SaveChangesAsync(token);
            prompt.ConversationId = conversation.Id;
        }

        return conversation;
    }

    private static List<MessagePart> GetMessagesParts(PromptAttachment[]? attachments)
    {
        return [];
    }

    private static string? MD5Hash(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        using var md5HashAlgo = MD5.Create();

        var hashBytes = md5HashAlgo.ComputeHash(Encoding.UTF8.GetBytes(userId));

        return BitConverter.ToString(hashBytes);
    }

    /// <summary>
    /// Build the user message, the prompt text followed by its attachments.
    /// </summary>
    private static UserChatMessage BuildUserMessage(PostPrompt prompt)
    {
        var parts = new List<ChatMessageContentPart>
        {
            ChatMessageContentPart.CreateTextPart(prompt.Prompt ?? "")
        };

        if (prompt.Attachments != null && prompt.Attachments.Length > 0)
        {
            foreach (var attachment in prompt.Attachments)
            {
                parts.Add(BuildAttachmentPart(attachment));
            }
        }

        return new UserChatMessage(parts);
    }

    private static ChatMessageContentPart BuildAttachmentPart(PromptAttachment attachment)
    {
        if (IsImage(attachment.ContentType))
        {
            if (!string.IsNullOrWhiteSpace(attachment.PublicUri) && Uri.IsWellFormedUriString(attachment.PublicUri, UriKind.Absolute))
            {
                return ChatMessageContentPart.CreateImagePart(new Uri(attachment.PublicUri));
            }

            using var memStream = new MemoryStream();

            var b64 = Convert.FromBase64String(attachment.ContentBase64);
            memStream.Write(b64, 0, b64.Length);

            return ChatMessageContentPart.CreateImagePart(
                BinaryData.FromStream(memStream),
                attachment.ContentType);
        }

        if (attachment.ContentType.ToLower() == "text/plain")
        {
            return ChatMessageContentPart.CreateTextPart(attachment.TextContent);
        }

        throw new NotImplementedException($"Le type de contenu {attachment.ContentType} n'est pas supporté pour les pièces jointes.");
    }

    private static bool IsImage(string contentType)
    {
        switch (contentType.ToLower())
        {
            case "image/png":
            case "image/jpeg":
            case "image/jpg":
            case "image/gif":
            case "image/bmp":
            case "image/tiff":
            case "image/webp":
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// What one prompt produced: the answer, and the tool calls it took to get there.
    /// </summary>
    private sealed record AiExchange(AIResponse? Answer, IReadOnlyList<ErabliereAiToolResult> ToolResults);
}
