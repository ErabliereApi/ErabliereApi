using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ErabliereApi.Donnees.Interfaces;

/// <summary>
/// Entité représentant une conversation entre avec ErabliereAI.
/// </summary>
public class Conversation : IIdentifiable<Guid?, Conversation>
{
    /// <summary>
    /// La clé primaire de la conversation.
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// L'id de l'utilisateur.
    /// </summary>
    [MaxLength(50)]
    public string? UserId { get; set; }

    /// <summary>
    /// Le nom de la conersation.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Prase système utilisé pour la conversation
    /// </summary>
    public string? SystemMessage { get; set; }

    /// <summary>
    /// Indique si la conversation est publique. Peut être consultée sans être authentifié.
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// La date de création de la conversation.
    /// </summary>
    public DateTimeOffset? CreatedOn { get; set; }

    /// <summary>
    /// La date du dernier message envoyé.
    /// </summary>
    public DateTimeOffset? LastMessageDate { get; set; }

    /// <summary>
    /// Liste des messages de la conversation.
    /// </summary>
    public List<Message>? Messages { get; set; }

    /// <summary>
    /// Comparer les conversations par date du dernier message.
    /// </summary>
    public int CompareTo(Conversation? other)
    {
        if (other == null)
        {
            return 1;
        }

        if (other.LastMessageDate == null)
        {
            return LastMessageDate == null ? 0 : -1;
        }

        return LastMessageDate?.CompareTo(other.LastMessageDate.Value) ?? 0;
    }
}
