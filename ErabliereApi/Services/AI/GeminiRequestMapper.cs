using Google.GenAI.Types;
using OpenAI.Chat;
using System.Text;
using System.Text.Json;

namespace ErabliereApi.Services.AI;

/// <summary>
/// Translates a conversation written with the OpenAI chat types — the contract
/// <see cref="IAIService" /> is expressed in — into what the Gemini content
/// generation api expects, and translates its answer back.
/// </summary>
/// <remarks>
/// The two apis disagree on three points, and each one is a way to hand the model
/// a subtly wrong conversation if it is glossed over:
/// <list type="bullet">
///   <item>the system prompt is a turn of the conversation for OpenAI, a field of
///         its own for Gemini;</item>
///   <item>a tool call and its result are messages for OpenAI, parts of a content
///         for Gemini;</item>
///   <item>OpenAI pairs a result with its call by id, Gemini by function name.</item>
/// </list>
/// Kept out of <see cref="GeminiAIService" /> so the translation can be tested
/// without an api key: it is the only part of that service which is not a call to
/// Google.
/// </remarks>
public static class GeminiRequestMapper
{
    /// <summary>
    /// The role of the caller. Gemini expects it on the results of the tools too:
    /// they are something handed <em>to</em> the model, not something it said.
    /// </summary>
    public const string UserRole = "user";

    /// <summary>
    /// The role of the model, which OpenAI calls the assistant.
    /// </summary>
    public const string ModelRole = "model";

    /// <summary>
    /// Prefix of the ids handed out when Gemini names none for a function call.
    /// </summary>
    /// <remarks>
    /// The tool loop is written on the OpenAI model, where a result names the id of
    /// the call it answers, and <see cref="ToolChatMessage" /> requires one. Gemini
    /// leaves the id empty outside of the live api, so one is invented here — and
    /// recognized on the way back, so an id Gemini never issued is never echoed to
    /// it.
    /// </remarks>
    public const string SyntheticToolCallIdPrefix = "gemini-call-";

    /// <summary>
    /// The key a tool result is wrapped under when it is not a json object. Gemini
    /// types a function response as an object, where OpenAI takes any text.
    /// </summary>
    public const string ResultKey = "result";

    private const string TypeKeyword = "type";
    private const string NullableKeyword = "nullable";
    private const string NullType = "null";

    /// <summary>
    /// The system messages of the conversation, joined into the single system
    /// instruction of the request. Null when there is none, so the field stays out.
    /// </summary>
    public static Content? MapSystemInstruction(IEnumerable<ChatMessage> messages)
    {
        var parts = messages.OfType<SystemChatMessage>()
                            .SelectMany(TextParts)
                            .ToList();

        return parts.Count == 0 ? null : new Content { Parts = parts };
    }

    /// <summary>
    /// The turns of the conversation, system messages excluded: they belong to
    /// <see cref="MapSystemInstruction" />, and replaying them here as a turn of the
    /// model would make the model read its own instructions as something it said.
    /// </summary>
    public static List<Content> MapContents(IEnumerable<ChatMessage> messages)
    {
        // The name Gemini pairs a result with, per id of the call that asked for it.
        // Filled as the conversation is walked, which is enough because a call is
        // always sent before its result.
        var toolNames = new Dictionary<string, string>(StringComparer.Ordinal);
        var contents = new List<Content>();

        foreach (var message in messages)
        {
            switch (message)
            {
                case SystemChatMessage:
                    continue;

                case AssistantChatMessage assistant when assistant.ToolCalls.Count > 0:
                    foreach (var toolCall in assistant.ToolCalls)
                    {
                        toolNames[toolCall.Id] = toolCall.FunctionName;
                    }

                    contents.Add(new Content { Role = ModelRole, Parts = ToolCallParts(assistant) });
                    break;

                case ToolChatMessage tool:
                    AppendToolResult(contents, tool, toolNames);
                    break;

                default:
                    var parts = TextParts(message);

                    // A content without a part is rejected by the api. A message left
                    // empty, or whose only part is an attachment this provider does
                    // not translate, simply does not become a turn.
                    if (parts.Count == 0)
                    {
                        continue;
                    }

                    contents.Add(new Content
                    {
                        Role = message is UserChatMessage ? UserRole : ModelRole,
                        Parts = parts
                    });
                    break;
            }
        }

        return contents;
    }

    /// <summary>
    /// Translates the tools declared on the request into Gemini function
    /// declarations. Returns null when the caller declared none, so a request
    /// without tools stays exactly what it was.
    /// </summary>
    public static List<Tool>? MapTools(ChatCompletionOptions chatCompletion)
    {
        if (chatCompletion.Tools == null || chatCompletion.Tools.Count == 0)
        {
            return null;
        }

        var declarations = chatCompletion.Tools
            .Select(tool => new FunctionDeclaration
            {
                Name = tool.FunctionName,
                Description = tool.FunctionDescription,
                Parameters = MapParameters(tool)
            })
            .ToList();

        return [new Tool { FunctionDeclarations = declarations }];
    }

    /// <summary>
    /// The tool calls of an answer, in the shape the loop drives both providers with.
    /// </summary>
    public static IReadOnlyList<AIToolCall> MapToolCalls(GenerateContentResponse response)
    {
        var functionCalls = response.FunctionCalls;

        if (functionCalls == null || functionCalls.Count == 0)
        {
            return [];
        }

        return [.. functionCalls.Select(functionCall => new AIToolCall(
            string.IsNullOrEmpty(functionCall.Id) ? NewSyntheticToolCallId() : functionCall.Id,
            functionCall.Name ?? "",
            functionCall.Args == null ? "" : JsonSerializer.Serialize(functionCall.Args)))];
    }

    /// <summary>
    /// The text of an answer, thoughts excluded.
    /// </summary>
    /// <remarks>
    /// Read from the parts rather than from <c>GenerateContentResponse.Text</c>:
    /// the answer that asks for a tool has no text part at all, and the answer of a
    /// thinking model carries parts the user was never meant to read.
    /// </remarks>
    public static string? MapText(GenerateContentResponse response)
    {
        var parts = response.Candidates?.FirstOrDefault()?.Content?.Parts;

        if (parts == null)
        {
            return null;
        }

        var text = string.Concat(parts.Where(part => part.Thought != true)
                                      .Select(part => part.Text));

        return string.IsNullOrEmpty(text) ? null : text;
    }

    /// <summary>
    /// What the model said on the turn it asked for tools: its text, if it wrote
    /// any, then one part per call.
    /// </summary>
    private static List<Part> ToolCallParts(AssistantChatMessage assistant)
    {
        var parts = TextParts(assistant);

        foreach (var toolCall in assistant.ToolCalls)
        {
            parts.Add(new Part
            {
                FunctionCall = new FunctionCall
                {
                    Id = IssuedByGemini(toolCall.Id),
                    Name = toolCall.FunctionName,
                    Args = ParseArguments(toolCall.FunctionArguments?.ToString())
                }
            });
        }

        return parts;
    }

    /// <summary>
    /// Adds the result of a tool to the conversation.
    /// </summary>
    /// <remarks>
    /// The results of a round are gathered into a single content, which is the shape
    /// Gemini documents for the parallel calls it just asked for, where the loop
    /// hands them over one message at a time.
    /// </remarks>
    private static void AppendToolResult(List<Content> contents, ToolChatMessage tool, Dictionary<string, string> toolNames)
    {
        var name = toolNames.GetValueOrDefault(tool.ToolCallId);

        if (string.IsNullOrEmpty(name))
        {
            // Nothing correlates this result to a call, and a nameless function
            // response is rejected. The loop always sends the call first, so this is
            // a conversation built elsewhere: the result is still handed over, as
            // text, rather than dropped.
            var text = TextParts(tool);

            if (text.Count > 0)
            {
                contents.Add(new Content { Role = UserRole, Parts = text });
            }

            return;
        }

        var part = new Part
        {
            FunctionResponse = new FunctionResponse
            {
                Id = IssuedByGemini(tool.ToolCallId),
                Name = name,
                Response = MapToolResult(tool)
            }
        };

        if (contents.Count > 0 &&
            contents[^1].Parts is { Count: > 0 } previousParts &&
            previousParts[^1].FunctionResponse != null)
        {
            previousParts.Add(part);

            return;
        }

        contents.Add(new Content { Role = UserRole, Parts = [part] });
    }

    /// <summary>
    /// The result of a tool, as the object Gemini types a function response with.
    /// A result that is not a json object is wrapped rather than lost.
    /// </summary>
    private static Dictionary<string, object> MapToolResult(ToolChatMessage tool)
    {
        var json = string.Concat(tool.Content.Select(part => part.Text));

        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            var result = JsonSerializer.Deserialize<JsonElement>(json);

            return result.ValueKind == JsonValueKind.Object
                ? result.Deserialize<Dictionary<string, object>>() ?? []
                : new Dictionary<string, object> { [ResultKey] = result };
        }
        catch (JsonException)
        {
            return new Dictionary<string, object> { [ResultKey] = json };
        }
    }

    private static Dictionary<string, object>? ParseArguments(string? argumentsJson)
    {
        if (string.IsNullOrWhiteSpace(argumentsJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(argumentsJson);
        }
        catch (JsonException)
        {
            // The model wrote them; they come back as it wrote them. Gemini answers
            // on arguments it cannot read, which is a round the loop recovers from.
            return null;
        }
    }

    /// <summary>
    /// The parameters of a tool, as the schema dialect of Gemini.
    /// </summary>
    /// <remarks>
    /// <see cref="Schema.FromJson" /> answers null — for the whole schema, not just
    /// the offending property — on a keyword it does not know, and a declaration
    /// without parameters is the worst possible outcome: the model is told the tool
    /// takes nothing, calls it empty handed, and the answer is built on a failure it
    /// was never told about. So the schema is normalized first, and a schema that
    /// still does not translate raises rather than being quietly emptied.
    /// </remarks>
    private static Schema? MapParameters(ChatTool tool)
    {
        var parameters = tool.FunctionParameters?.ToString();

        if (string.IsNullOrWhiteSpace(parameters))
        {
            return null;
        }

        var normalized = Normalize(JsonDocument.Parse(parameters).RootElement);

        return Schema.FromJson(normalized)
            ?? throw new InvalidOperationException(
                $"The parameters of the tool '{tool.FunctionName}' could not be translated into a Gemini schema: {normalized}");
    }

    /// <summary>
    /// Rewrites a json schema into what Gemini reads.
    /// </summary>
    /// <remarks>
    /// One difference so far, and it covers every optional parameter of the tool set:
    /// an optional parameter is typed <c>["integer", "null"]</c> by the schema
    /// generator, a union Gemini does not know, where it spells the same thing as a
    /// type and a <c>nullable</c> flag.
    /// </remarks>
    private static string Normalize(JsonElement schema)
    {
        using var buffer = new MemoryStream();

        using (var writer = new Utf8JsonWriter(buffer))
        {
            WriteNormalized(schema, writer);
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static void WriteNormalized(JsonElement element, Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();

                foreach (var property in element.EnumerateObject())
                {
                    if (property.NameEquals(TypeKeyword) && property.Value.ValueKind == JsonValueKind.Array)
                    {
                        WriteUnionType(property.Value, writer);

                        continue;
                    }

                    if (property.NameEquals(TypeKeyword) && property.Value.ValueKind == JsonValueKind.String)
                    {
                        WriteType(property.Value.GetString(), nullable: false, writer);

                        continue;
                    }

                    writer.WritePropertyName(property.Name);
                    WriteNormalized(property.Value, writer);
                }

                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();

                foreach (var item in element.EnumerateArray())
                {
                    WriteNormalized(item, writer);
                }

                writer.WriteEndArray();
                break;

            default:
                element.WriteTo(writer);
                break;
        }
    }

    private static void WriteUnionType(JsonElement types, Utf8JsonWriter writer)
    {
        string? type = null;
        var nullable = false;

        foreach (var candidate in types.EnumerateArray())
        {
            var name = candidate.GetString();

            if (string.Equals(name, NullType, StringComparison.OrdinalIgnoreCase))
            {
                nullable = true;

                continue;
            }

            type ??= name;
        }

        WriteType(type, nullable, writer);
    }

    /// <summary>
    /// Writes the type of a schema node, upper cased.
    /// </summary>
    /// <remarks>
    /// Json schema spells it <c>integer</c>, Gemini <c>INTEGER</c>: the field is an
    /// enum of the underlying proto, and the sdk exposes its values upper cased. The
    /// generated schemas are in the json schema spelling, so they are translated
    /// here rather than sent in a case the api is not documented to read.
    /// </remarks>
    private static void WriteType(string? type, bool nullable, Utf8JsonWriter writer)
    {
        if (type != null)
        {
            writer.WriteString(TypeKeyword, type.ToUpperInvariant());
        }

        if (nullable)
        {
            writer.WriteBoolean(NullableKeyword, true);
        }
    }

    private static List<Part> TextParts(ChatMessage message)
    {
        // Only the text is carried over: an attachment is turned into an image part
        // by the caller, which this provider does not translate, and an empty part
        // is rejected by the api.
        return [.. message.Content.Where(part => !string.IsNullOrEmpty(part.Text))
                                  .Select(part => new Part { Text = part.Text })];
    }

    private static string NewSyntheticToolCallId()
    {
        return $"{SyntheticToolCallIdPrefix}{Guid.NewGuid():N}";
    }

    /// <summary>
    /// The id to echo to Gemini: the one it issued, or nothing when the id it reads
    /// was invented here.
    /// </summary>
    private static string? IssuedByGemini(string toolCallId)
    {
        return string.IsNullOrEmpty(toolCallId) || toolCallId.StartsWith(SyntheticToolCallIdPrefix, StringComparison.Ordinal)
            ? null
            : toolCallId;
    }
}
