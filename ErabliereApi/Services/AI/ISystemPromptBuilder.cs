namespace ErabliereApi.Services.AI;

/// <summary>
/// Builds the system prompt used by ErabliereAI conversations.
///
/// This is the single place where the system prompt is composed. Enriching the
/// prompt (tool descriptions, erabliere context, ...) happens here and nowhere else.
/// </summary>
public interface ISystemPromptBuilder
{
    /// <summary>
    /// The system prompt used when neither the conversation nor the request
    /// specifies one.
    /// </summary>
    string DefaultSystemPrompt { get; }

    /// <summary>
    /// The system prompt to store on a conversation being created.
    /// </summary>
    /// <param name="requestedSystemMessage">The system message sent by the client, may be null or blank.</param>
    string BuildForNewConversation(string? requestedSystemMessage);

    /// <summary>
    /// The system prompt to send to the LLM for an existing conversation.
    /// </summary>
    /// <param name="conversationSystemMessage">The system message stored on the conversation, may be null or blank.</param>
    string BuildForCompletion(string? conversationSystemMessage);
}
