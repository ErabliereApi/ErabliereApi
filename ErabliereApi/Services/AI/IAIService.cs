using OpenAI.Chat;

namespace ErabliereApi.Services.AI;

/// <summary>
/// Interface for abstracting AI services
/// </summary>
public interface IAIService
{
    /// <summary>
    /// Whether this provider can be driven through a tool calling loop: it must both
    /// report the tool calls it wants on <see cref="AIResponse.ToolCalls" /> and accept
    /// the assistant and tool messages carrying their results on the next turn.
    /// </summary>
    /// <remarks>
    /// False is the safe answer. A provider that says false is simply never sent a
    /// tool definition, and the chat keeps answering from the model's own knowledge
    /// exactly as it did before tool calling existed.
    /// <para>
    /// Declared without a default implementation on purpose: a default interface
    /// member cannot be proxied, so every test double would throw on the very
    /// property that decides whether the tools are used.
    /// </para>
    /// </remarks>
    bool SupportsToolCalling { get; }

    /// <summary>
    /// Complete a chat with an LLM
    /// </summary>
    /// <param name="messages"></param>
    /// <param name="chatCompletion"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task<AIResponse?> CompleteChatAsync(IEnumerable<ChatMessage> messages, ChatCompletionOptions chatCompletion, CancellationToken token);
}
