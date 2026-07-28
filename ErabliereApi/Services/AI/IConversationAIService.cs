using ErabliereApi.Donnees.Action.Post;

namespace ErabliereApi.Services.AI;

/// <summary>
/// Orchestrates an ErabliereAI prompt: conversation resolution, prompt building,
/// LLM call and message persistence.
/// </summary>
public interface IConversationAIService
{
    /// <summary>
    /// Send a prompt to the LLM and persist both the question and the answer.
    /// </summary>
    /// <param name="prompt">The prompt sent by the client. Its ConversationId is filled when a conversation is created.</param>
    /// <param name="userId">The unique name of the caller, used as the owner of a new conversation.</param>
    /// <param name="token"></param>
    /// <exception cref="AIChatCompletionException">The provider rejected the chat completion request.</exception>
    Task<PostPromptResponse> SendPromptAsync(PostPrompt prompt, string? userId, CancellationToken token);
}
