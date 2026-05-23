using OpenAI.Chat;
using System.ClientModel;
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
                PresencePenalty = chatCompletion.PresencePenalty
            },
            token);

        return new AIResponse
        {
            Text = response.Text
        };
    }
}
