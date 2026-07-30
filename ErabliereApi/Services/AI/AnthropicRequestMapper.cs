using Anthropic.Models.Messages;
using OpenAI.Chat;
using System.Text;
using System.Text.Json;

namespace ErabliereApi.Services.AI;

/// <summary>
/// Translates a conversation written with the OpenAI chat types — the contract
/// <see cref="IAIService" /> is expressed in — into what the Anthropic messages
/// api expects, and translates its answer back.
/// </summary>
/// <remarks>
/// The two apis disagree on three points, and each one is a way to hand the model
/// a subtly wrong conversation if it is glossed over:
/// <list type="bullet">
///   <item>the system prompt is a turn of the conversation for OpenAI, a
///         parameter of its own for Anthropic;</item>
///   <item>a tool call and its result are messages for OpenAI, content blocks
///         (<c>tool_use</c> / <c>tool_result</c>) for Anthropic — and the results
///         of a round must all sit in a single user message;</item>
///   <item>Anthropic pairs a result with its call by <c>tool_use_id</c>, exactly
///         like OpenAI pairs by id — which is why, unlike
///         <see cref="GeminiRequestMapper" />, no synthetic id machinery exists
///         here: the <c>toolu_…</c> ids round-trip untouched.</item>
/// </list>
/// Kept out of <see cref="AnthropicAIService" /> so the translation can be tested
/// without an api key: it is the only part of that service which is not a call to
/// Anthropic.
/// </remarks>
public static class AnthropicRequestMapper
{
    /// <summary>
    /// The system messages of the conversation, joined into the system parameter
    /// of the request. Null when there is none, so the field stays out. This
    /// absorbs the initial system prompt and the limit-reached instruction the
    /// tool loop appends mid-conversation alike.
    /// </summary>
    public static List<TextBlockParam>? MapSystem(IEnumerable<ChatMessage> messages)
    {
        var blocks = messages.OfType<SystemChatMessage>()
                             .SelectMany(message => message.Content)
                             .Where(part => !string.IsNullOrEmpty(part.Text))
                             .Select(part => new TextBlockParam { Text = part.Text })
                             .ToList();

        return blocks.Count == 0 ? null : blocks;
    }

    /// <summary>
    /// The turns of the conversation, system messages excluded: they belong to
    /// <see cref="MapSystem" />, and replaying them here as a turn of the model
    /// would make the model read its own instructions as something it said.
    /// </summary>
    public static List<MessageParam> MapMessages(IEnumerable<ChatMessage> messages)
    {
        // The ids of the calls the conversation carried so far. Anthropic rejects
        // a tool_result whose id matches no tool_use, so a result is only sent as
        // one when its call was actually sent.
        var seenToolCallIds = new HashSet<string>(StringComparer.Ordinal);
        var mapped = new List<MessageParam>();

        // The results of the round being walked. Anthropic documents the results
        // of parallel calls as blocks of a single user message, where the loop
        // hands them over one message at a time: they are gathered here and
        // flushed as one turn.
        var pendingToolResults = new List<ContentBlockParam>();

        foreach (var message in messages)
        {
            switch (message)
            {
                case SystemChatMessage:
                    continue;

                case AssistantChatMessage assistant when assistant.ToolCalls.Count > 0:
                    FlushToolResults(mapped, pendingToolResults);

                    foreach (var toolCall in assistant.ToolCalls)
                    {
                        seenToolCallIds.Add(toolCall.Id);
                    }

                    mapped.Add(new MessageParam { Role = Role.Assistant, Content = ToolCallBlocks(assistant) });
                    break;

                case ToolChatMessage tool when seenToolCallIds.Contains(tool.ToolCallId):
                    pendingToolResults.Add(new ToolResultBlockParam
                    {
                        ToolUseID = tool.ToolCallId,
                        Content = string.Concat(tool.Content.Select(part => part.Text))
                    });
                    break;

                case ToolChatMessage orphan:
                    // Nothing correlates this result to a call, and Anthropic
                    // rejects an unmatched tool_result. The loop always sends the
                    // call first, so this is a conversation built elsewhere: the
                    // result is still handed over, as text, rather than dropped.
                    FlushToolResults(mapped, pendingToolResults);

                    var text = ContentBlocks(orphan);

                    if (text.Count > 0)
                    {
                        mapped.Add(new MessageParam { Role = Role.User, Content = text });
                    }

                    break;

                default:
                    FlushToolResults(mapped, pendingToolResults);

                    var blocks = ContentBlocks(message);

                    // A message without a block is rejected by the api. A message
                    // left empty simply does not become a turn.
                    if (blocks.Count == 0)
                    {
                        continue;
                    }

                    mapped.Add(new MessageParam
                    {
                        Role = message is UserChatMessage ? Role.User : Role.Assistant,
                        Content = blocks
                    });
                    break;
            }
        }

        FlushToolResults(mapped, pendingToolResults);

        return mapped;
    }

    /// <summary>
    /// Translates the tools declared on the request into Anthropic tools. Returns
    /// null when the caller declared none, so a request without tools stays
    /// exactly what it was.
    /// </summary>
    /// <remarks>
    /// Anthropic reads standard json schema, so the translation is a near
    /// passthrough: the generated schemas are split into their
    /// <c>properties</c> and <c>required</c> members and handed over untouched —
    /// including the <c>["integer", "null"]</c> type unions of the optional
    /// parameters, which <see cref="GeminiRequestMapper" /> has to rewrite.
    /// </remarks>
    public static List<ToolUnion>? MapTools(ChatCompletionOptions chatCompletion)
    {
        if (chatCompletion.Tools == null || chatCompletion.Tools.Count == 0)
        {
            return null;
        }

        return [.. chatCompletion.Tools.Select(tool => new ToolUnion(new Tool
        {
            Name = tool.FunctionName,
            Description = tool.FunctionDescription,
            InputSchema = MapInputSchema(tool)
        }))];
    }

    /// <summary>
    /// The tool calls of an answer, in the shape the loop drives every provider
    /// with. The arguments come back as a json object string, which is what the
    /// toolset parses on invocation.
    /// </summary>
    public static IReadOnlyList<AIToolCall> MapToolCalls(Message response)
    {
        var toolCalls = new List<AIToolCall>();

        foreach (var block in response.Content)
        {
            if (block.TryPickToolUse(out ToolUseBlock? toolUse))
            {
                toolCalls.Add(new AIToolCall(
                    toolUse.ID,
                    toolUse.Name,
                    toolUse.Input == null ? "{}" : JsonSerializer.Serialize(toolUse.Input)));
            }
        }

        return toolCalls;
    }

    /// <summary>
    /// The text of an answer, thinking excluded.
    /// </summary>
    /// <remarks>
    /// Only the text blocks are read: the answer that asks for a tool has no text
    /// block at all, and the thinking blocks of a reasoning model carry content
    /// the user was never meant to read.
    /// </remarks>
    public static string? MapText(Message response)
    {
        var text = new StringBuilder();

        foreach (var block in response.Content)
        {
            if (block.TryPickText(out TextBlock? textBlock))
            {
                text.Append(textBlock.Text);
            }
        }

        return text.Length == 0 ? null : text.ToString();
    }

    /// <summary>
    /// The refusal of an answer, when the model declined to write one. Anthropic
    /// reports it as a stop reason rather than as a message, so it is read from
    /// there and persisted the way the other providers' refusals are.
    /// </summary>
    public static string? MapRefusal(Message response)
    {
        if (!string.Equals(response.StopReason?.ToString(), "refusal", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var explanation = response.StopDetails?.Explanation;

        return string.IsNullOrWhiteSpace(explanation) ? "refusal" : explanation;
    }

    /// <summary>
    /// What the model said on the turn it asked for tools: its text, if it wrote
    /// any, then one tool_use block per call.
    /// </summary>
    private static List<ContentBlockParam> ToolCallBlocks(AssistantChatMessage assistant)
    {
        var blocks = ContentBlocks(assistant);

        foreach (var toolCall in assistant.ToolCalls)
        {
            blocks.Add(new ToolUseBlockParam
            {
                ID = toolCall.Id,
                Name = toolCall.FunctionName,
                Input = ParseArguments(toolCall.FunctionArguments?.ToString())
            });
        }

        return blocks;
    }

    /// <summary>
    /// Flushes the tool results of a round into the single user message Anthropic
    /// expects them in.
    /// </summary>
    private static void FlushToolResults(List<MessageParam> mapped, List<ContentBlockParam> pendingToolResults)
    {
        if (pendingToolResults.Count == 0)
        {
            return;
        }

        mapped.Add(new MessageParam { Role = Role.User, Content = new List<ContentBlockParam>(pendingToolResults) });
        pendingToolResults.Clear();
    }

    /// <summary>
    /// The content blocks of a plain message: its text, and its images. Anthropic
    /// reads both the url and the base64 form the attachments arrive in, so an
    /// image reaches the model here the way it does with Azure OpenAI.
    /// </summary>
    private static List<ContentBlockParam> ContentBlocks(ChatMessage message)
    {
        var blocks = new List<ContentBlockParam>();

        foreach (var part in message.Content)
        {
            if (!string.IsNullOrEmpty(part.Text))
            {
                blocks.Add(new TextBlockParam { Text = part.Text });
            }
            else if (part.Kind == ChatMessageContentPartKind.Image)
            {
                if (part.ImageUri != null)
                {
                    blocks.Add(new ImageBlockParam
                    {
                        Source = new UrlImageSource { Url = part.ImageUri.ToString() }
                    });
                }
                else if (part.ImageBytes != null)
                {
                    blocks.Add(new ImageBlockParam
                    {
                        Source = new Base64ImageSource
                        {
                            Data = Convert.ToBase64String(part.ImageBytes.ToArray()),
                            MediaType = part.ImageBytesMediaType
                        }
                    });
                }
            }
        }

        return blocks;
    }

    /// <summary>
    /// The parameters of a tool, split into the members of an Anthropic input
    /// schema. The root <c>type</c> is set by the sdk itself.
    /// </summary>
    /// <remarks>
    /// A schema whose root is not an object schema raises rather than being
    /// quietly emptied: a declaration without parameters is the worst possible
    /// outcome — the model is told the tool takes nothing, calls it empty handed,
    /// and the answer is built on a failure it was never told about.
    /// </remarks>
    private static InputSchema MapInputSchema(ChatTool tool)
    {
        var parameters = tool.FunctionParameters?.ToString();

        if (string.IsNullOrWhiteSpace(parameters))
        {
            return new InputSchema { Properties = new Dictionary<string, JsonElement>() };
        }

        using var document = JsonDocument.Parse(parameters);
        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object ||
            (root.TryGetProperty("type", out var type) && !string.Equals(type.GetString(), "object", StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                $"The parameters of the tool '{tool.FunctionName}' could not be translated into an Anthropic input schema: {parameters}");
        }

        var properties = new Dictionary<string, JsonElement>();

        if (root.TryGetProperty("properties", out var declared) && declared.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in declared.EnumerateObject())
            {
                // Cloned so nothing dangles once the document is disposed.
                properties[property.Name] = property.Value.Clone();
            }
        }

        List<string>? required = null;

        if (root.TryGetProperty("required", out var names) && names.ValueKind == JsonValueKind.Array)
        {
            required = [.. names.EnumerateArray()
                                .Select(name => name.GetString())
                                .Where(name => !string.IsNullOrEmpty(name))
                                .Cast<string>()];
        }

        return new InputSchema { Properties = properties, Required = required };
    }

    /// <summary>
    /// The arguments of a call, as the object Anthropic types a tool_use input
    /// with. Arguments the model wrote unreadably come back as an empty object:
    /// Anthropic requires one, and the loop recovers from the round.
    /// </summary>
    private static Dictionary<string, JsonElement> ParseArguments(string? argumentsJson)
    {
        if (string.IsNullOrWhiteSpace(argumentsJson))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argumentsJson) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
