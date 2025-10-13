using System;

namespace ErabliereApi.Donnees.Action.Post;

/// <summary>
/// Représente un prompt
/// </summary>
public class PostPrompt
{
    /// <summary>
    /// Phrase système utilisé lors de Prompt de type Chat
    /// </summary>
    public string? SystemMessage { get; set; }

    /// <summary>
    /// Le prompt
    /// </summary>
    public string? Prompt { get; set; }

    /// <summary>
    /// L'identifiant de la conversation
    /// </summary>
    public Guid? ConversationId { get; set; }

    /// <summary>
    /// Le type de prompt Chat ou Completion
    /// Si laisser vide, par défaut Completion sera utilisé
    /// </summary>
    public string? PromptType { get; set; }

    /// <summary>
    /// Le nombre de token maximum. 
    /// Si laisser à null, par défaut 800 sera utilisé
    /// </summary>
    public int? MaxToken { get; set; }

    /// <summary>
    /// Les pièces jointes du prompt
    /// </summary>
    public PromptAttachment[]? Attachments { get; set; }
}

/// <summary>
/// 
/// </summary>
public class PromptAttachment
{
    /// <summary>
    /// Le nom du fichier
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Uri publique du fichier
    /// </summary>
    public string? PublicUri { get; set; }

    /// <summary>
    /// Gets or sets the MIME type of the content.
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Le contenu du fichier encodé en base64
    /// </summary>
    public string ContentBase64 { get; set; } = string.Empty;

    /// <summary>
    /// Text content
    /// </summary>
    public string? TextContent { get; set; }
}