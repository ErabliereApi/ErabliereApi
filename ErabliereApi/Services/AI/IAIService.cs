using OpenAI.Chat;

namespace ErabliereApi.Services.AI;

/// <summary>
/// Interface for abstracting AI services
/// </summary>
public interface IAIService
{
    /// <summary>
    /// Complete a chat with an LLM
    /// </summary>
    /// <param name="messages"></param>
    /// <param name="chatCompletion"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task<AIResponse?> CompleteChatAsync(IEnumerable<ChatMessage> messages, ChatCompletionOptions chatCompletion, CancellationToken token);
}