using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Donnees.Action.Post;
using ErabliereApi.Extensions;
using Microsoft.EntityFrameworkCore;
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

    private readonly ErabliereDbContext _depot;
    private readonly IConfiguration _configuration;
    private readonly IAIService _aiService;
    private readonly ISystemPromptBuilder _systemPromptBuilder;

    /// <summary>
    /// Constructeur par initialisation
    /// </summary>
    /// <param name="depot"></param>
    /// <param name="configuration"></param>
    /// <param name="aiService"></param>
    /// <param name="systemPromptBuilder"></param>
    public ConversationAIService(
        ErabliereDbContext depot,
        IConfiguration configuration,
        IAIService aiService,
        ISystemPromptBuilder systemPromptBuilder)
    {
        _depot = depot;
        _configuration = configuration;
        _aiService = aiService;
        _systemPromptBuilder = systemPromptBuilder;
    }

    /// <inheritdoc />
    public async Task<PostPromptResponse> SendPromptAsync(PostPrompt prompt, string? userId, CancellationToken token)
    {
        var conversation = await GetOrCreateConversationAsync(prompt, userId, token);

        var aiResponse = prompt.PromptType == ChatPromptType ?
            await CompleteChatAsync(prompt, conversation, token) :
            await CompleteSinglePromptAsync(prompt, conversation, token);

        var response = await PersistExchangeAsync(prompt, aiResponse, token);

        return new PostPromptResponse
        {
            Prompt = prompt,
            Conversation = conversation,
            Response = response,
        };
    }

    /// <summary>
    /// Complete a prompt using the whole history of the conversation.
    /// </summary>
    private async Task<AIResponse?> CompleteChatAsync(PostPrompt prompt, Conversation conversation, CancellationToken token)
    {
        var messages = await _depot.Messages
            .Where(m => m.ConversationId == prompt.ConversationId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(token);

        var messagesPrompt = new List<ChatMessage>
        {
            new SystemChatMessage(_systemPromptBuilder.BuildForCompletion(conversation.SystemMessage))
        };

        foreach (var message in messages)
        {
            messagesPrompt.Add(message.IsUser ?
                new UserChatMessage(message.Content) :
                new AssistantChatMessage(message.Content));
        }

        messagesPrompt.Add(BuildUserMessage(prompt));

        try
        {
            return await _aiService.CompleteChatAsync(messagesPrompt, BuildCompletionOptions(conversation), token);
        }
        catch (ClientResultException e)
        {
            throw new AIChatCompletionException(e);
        }
    }

    /// <summary>
    /// Complete a prompt without any conversation history.
    /// </summary>
    private Task<AIResponse?> CompleteSinglePromptAsync(PostPrompt prompt, Conversation conversation, CancellationToken token)
    {
        return _aiService.CompleteChatAsync([prompt.Prompt], BuildCompletionOptions(conversation), token);
    }

    private ChatCompletionOptions BuildCompletionOptions(Conversation conversation)
    {
        return new ChatCompletionOptions
        {
            Temperature = _configuration.GetRequiredValue<float>("LLMDefaultTemperature"),
            FrequencyPenalty = 0,
            PresencePenalty = 0,
            EndUserId = MD5Hash(conversation.UserId)
        };
    }

    /// <summary>
    /// Persist the question and the answer, and return the persisted answer.
    /// </summary>
    private async Task<Message> PersistExchangeAsync(PostPrompt prompt, AIResponse? aiResponse, CancellationToken token)
    {
        var query = new Message
        {
            ConversationId = prompt.ConversationId,
            Content = prompt.Prompt ?? "",
            IsUser = true,
            CreatedAt = DateTime.Now,
            MessageParts = GetMessagesParts(prompt.Attachments)
        };

        var response = new Message
        {
            ConversationId = prompt.ConversationId,
            Content = aiResponse?.Text ?? "Aucune réponse",
            IsUser = false,
            CreatedAt = DateTime.Now,
            Refusal = aiResponse?.Refusal,
            ImageUri = aiResponse?.Kind == ChatMessageContentPartKind.Image.ToString() ? aiResponse?.ImageUri?.ToString() : null
        };

        await _depot.Messages.AddAsync(query, token);
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
}
