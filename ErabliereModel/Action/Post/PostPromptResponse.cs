namespace ErabliereApi.Donnees.Action.Post;

/// <summary>
/// Réponse à un prompt
/// </summary>
public class PostPromptResponse
{
    /// <summary>
    /// La requête du prompt
    /// </summary>
    public PostPrompt? Prompt { get; set; }

    /// <summary>
    /// La conversation
    /// </summary>
    public Conversation? Conversation { get; set; }

    /// <summary>
    /// La réponse
    /// </summary>
    public Message? Response { get; set; }
}
