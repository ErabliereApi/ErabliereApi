using OpenAI.Chat;
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
    /// Both halves of the loop are implemented by <see cref="GeminiRequestMapper" />:
    /// the tool declarations and the function calls of the answer are translated, and
    /// so are the assistant and tool messages carrying the results on the next turn.
    /// Set <c>GoogleGenAIEnableToolCalling</c> to "false" to take the tools away from
    /// this provider without changing the rest of the configuration; the chat then
    /// answers from the model's own knowledge, as it did before tool calling existed.
    /// </remarks>
    public bool SupportsToolCalling =>
        !string.Equals(_config["GoogleGenAIEnableToolCalling"], "false", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public async Task<AIResponse?> CompleteChatAsync(IEnumerable<ChatMessage> messages, ChatCompletionOptions chatCompletion, CancellationToken token)
    {
        var gemini = new Client(apiKey: _config["GoogleGenAIKey"]);

        var response = await gemini.Models.GenerateContentAsync(
            _config["GoogleGenAIModel"] ?? "gemini-2.5-flash",
            GeminiRequestMapper.MapContents(messages),
            new GenerateContentConfig
            {
                Temperature = chatCompletion.Temperature,
                FrequencyPenalty = chatCompletion.FrequencyPenalty,
                PresencePenalty = chatCompletion.PresencePenalty,
                SystemInstruction = GeminiRequestMapper.MapSystemInstruction(messages),
                Tools = GeminiRequestMapper.MapTools(chatCompletion)
            },
            token);

        return new AIResponse
        {
            Text = GeminiRequestMapper.MapText(response),
            FinishReason = response.Candidates?.FirstOrDefault()?.FinishReason?.ToString(),
            ToolCalls = GeminiRequestMapper.MapToolCalls(response)
        };
    }
}
