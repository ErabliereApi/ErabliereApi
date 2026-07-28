namespace ErabliereApi.Services.AI;

/// <summary>
/// Default <see cref="ISystemPromptBuilder" /> implementation.
/// </summary>
public class SystemPromptBuilder : ISystemPromptBuilder
{
    /// <inheritdoc />
    public string DefaultSystemPrompt => "Vous êtes un acériculteur expérimenté avec des connaissance scientifique et pratique.";

    /// <inheritdoc />
    public string BuildForNewConversation(string? requestedSystemMessage)
    {
        return string.IsNullOrWhiteSpace(requestedSystemMessage) ?
            DefaultSystemPrompt :
            requestedSystemMessage;
    }

    /// <inheritdoc />
    public string BuildForCompletion(string? conversationSystemMessage)
    {
        return string.IsNullOrWhiteSpace(conversationSystemMessage) ?
            DefaultSystemPrompt :
            conversationSystemMessage;
    }
}
