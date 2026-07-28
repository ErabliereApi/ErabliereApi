using OpenAI.Chat;
using System.Text.Json;
using Google.GenAI;
using Google.GenAI.Types;

namespace ErabliereApi.Services.AI;

/// <summary>
/// Service pour interagir avec l'api de Gemini
/// </summary>
public class GeminiAIService : IAIService
{
    private readonly IConfiguration _config;

    /// <summary>
    /// Constructeur
    /// </summary>
    /// <param name="config"></param>
    public GeminiAIService(IConfiguration config)
    {
        _config = config;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Off unless <c>GoogleGenAIEnableToolCalling</c> is set to "true". The outbound half
    /// of the loop is implemented — the tool declarations and the function calls of the
    /// answer are translated both ways — but the return leg is not: a
    /// <see cref="ToolChatMessage" /> carrying a tool result is mapped here like any other
    /// message, not as the function response part Gemini expects. Rather than feed the
    /// model a subtly wrong conversation, the chat degrades to no tools at all on this
    /// provider, which is the phase 7 behaviour and never breaks. Flip the switch once the
    /// mapping below understands tool messages.
    /// </remarks>
    public bool SupportsToolCalling =>
        string.Equals(_config["GoogleGenAIEnableToolCalling"], "true", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    /// <remarks>
    /// The tools declared on <paramref name="chatCompletion" /> are translated into Gemini
    /// function declarations, and the function calls of the answer are translated back into
    /// <see cref="AIResponse.ToolCalls" />, so both providers expose the same contract.
    /// </remarks>
    public async Task<AIResponse?> CompleteChatAsync(IEnumerable<ChatMessage> messages, ChatCompletionOptions chatCompletion, CancellationToken token)
    {
        var gemini = new Client(apiKey: _config["GoogleGenAIKey"]);

        var response = await gemini.Models.GenerateContentAsync(
            _config["GoogleGenAIModel"] ?? "gemini-2.5-flash",
            messages.Select(m =>
            {
                return new Content
                {
                    Role = m.GetType().Name == "UserChatMessage" ? "user" : "model",
                    Parts = m.Content.Select(c =>
                    {
                        return new Part
                        {
                            Text = c.Text
                        };
                    }).ToList()
                };
            }).ToList(),
            new GenerateContentConfig
            {
                Temperature = chatCompletion.Temperature,
                FrequencyPenalty = chatCompletion.FrequencyPenalty,
                PresencePenalty = chatCompletion.PresencePenalty,
                Tools = MapTools(chatCompletion)
            },
            token);

        return new AIResponse
        {
            Text = response.Text,
            FinishReason = response.Candidates?.FirstOrDefault()?.FinishReason?.ToString(),
            ToolCalls = MapToolCalls(response)
        };
    }

    /// <summary>
    /// Translate the OpenAI tool declarations into Gemini function declarations.
    /// Returns null when the caller declared no tool, so the request stays unchanged.
    /// </summary>
    private static List<Tool>? MapTools(ChatCompletionOptions chatCompletion)
    {
        if (chatCompletion.Tools == null || chatCompletion.Tools.Count == 0)
        {
            return null;
        }

        var declarations = chatCompletion.Tools
            .Select(t => new FunctionDeclaration
            {
                Name = t.FunctionName,
                Description = t.FunctionDescription,
                Parameters = MapParameters(t)
            })
            .ToList();

        return [new Tool { FunctionDeclarations = declarations }];
    }

    private static Schema? MapParameters(ChatTool tool)
    {
        var parameters = tool.FunctionParameters?.ToString();

        if (string.IsNullOrWhiteSpace(parameters))
        {
            return null;
        }

        return Schema.FromJson(parameters);
    }

    private static IReadOnlyList<AIToolCall> MapToolCalls(GenerateContentResponse response)
    {
        var functionCalls = response.FunctionCalls;

        if (functionCalls == null || functionCalls.Count == 0)
        {
            return [];
        }

        return [.. functionCalls.Select(fc => new AIToolCall(
            fc.Id ?? "",
            fc.Name ?? "",
            fc.Args == null ? "" : JsonSerializer.Serialize(fc.Args)))];
    }
}
