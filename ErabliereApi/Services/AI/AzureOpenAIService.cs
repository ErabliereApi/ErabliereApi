using Azure;
using Azure.AI.OpenAI;
using OpenAI.Chat;

namespace ErabliereApi.Services.AI;

/// <summary>
/// IAIService for AzureOpenAIClient
/// </summary>
public class AzureOpenAIService : IAIService
{
    private readonly AzureOpenAIClient _client;
    private readonly IConfiguration _config;
    private readonly ChatClient _chat;

    /// <summary>
    /// Constructeur
    /// </summary>
    /// <param name="config"></param>
    public AzureOpenAIService(IConfiguration config)
    {
        _client = new AzureOpenAIClient(
            new Uri(config["AzureOpenAIUri"] ?? ""),
            new AzureKeyCredential(config["AzureOpenAIKey"] ?? "")
        );
        _config = config;
        _chat = _client.GetChatClient(_config["AzureOpenAIDeploymentChatModelName"]);
    }

    /// <inheritdoc />
    /// <remarks>
    /// <paramref name="chatCompletion" /> is forwarded as is, so the tools and the tool
    /// choice declared by the caller reach the model untouched.
    /// </remarks>
    public async Task<AIResponse?> CompleteChatAsync(IEnumerable<ChatMessage> messages, ChatCompletionOptions chatCompletion, CancellationToken token)
    {
        var result = await _chat.CompleteChatAsync(messages, chatCompletion, token);

        var c = result.Value?.Content?.FirstOrDefault();

        return new AIResponse
        {
            Text = c?.Text,
            Refusal = c?.Refusal,
            ImageUri = c?.ImageUri,
            Kind = c?.Kind.ToString(),
            FinishReason = result.Value?.FinishReason.ToString(),
            ToolCalls = MapToolCalls(result.Value)
        };
    }

    private static IReadOnlyList<AIToolCall> MapToolCalls(ChatCompletion? completion)
    {
        if (completion?.ToolCalls == null || completion.ToolCalls.Count == 0)
        {
            return [];
        }

        return [.. completion.ToolCalls.Select(tc => new AIToolCall(
            tc.Id,
            tc.FunctionName,
            tc.FunctionArguments?.ToString() ?? ""))];
    }
}
