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

    /// <summary>
    /// L'identifiant de l'érablière que l'utilisateur consulte au moment d'écrire le
    /// prompt, lorsque la conversation est ouverte à partir d'une page d'érablière.
    ///
    /// Cette valeur est un indice donné au modèle pour qu'il cesse de deviner les
    /// identifiants; ce n'est jamais une autorisation. Les outils appelés ensuite
    /// sont authentifiés comme l'appelant et l'API refuse ce qu'il ne possède pas.
    /// </summary>
    public Guid? ErabliereId { get; set; }

    /// <summary>
    /// Le nom de l'érablière consultée, utilisé uniquement pour rendre la phrase
    /// système lisible.
    /// </summary>
    public string? ErabliereNom { get; set; }

    /// <summary>
    /// Identifiant généré par le client pour suivre l'activité des outils pendant que
    /// la réponse se construit. Voir GET /ErabliereAI/Prompt/Status/{activityId}.
    /// </summary>
    public Guid? ActivityId { get; set; }
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