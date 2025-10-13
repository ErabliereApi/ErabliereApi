using ErabliereApi.Donnees.Interfaces;
using System;

namespace ErabliereApi.Donnees;

/// <summary>
/// Partie d'un message, peut être un fichier attaché ou une portion de texte
/// </summary>
public class MessagePart : IIdentifiable<Guid, MessagePart>
{
    /// <inheritdoc />
    public Guid Id { get; set; }

    /// <summary>
    /// Id du message parent
    /// </summary>
    public Guid MessageId { get; set; }

    /// <summary>
    /// Le contenu du message
    /// </summary>
    public string Content { get; set; } = "";

    /// <summary>
    /// Le contenu du message en byte array
    /// </summary>
    public byte[] ContentByte { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Le type de contenu (MIME)
    /// </summary>
    public string ContentType { get; set; } = "";

    /// <inheritdoc/>
    public int CompareTo(MessagePart? other)
    {
        return Id.CompareTo(other?.Id);
    }
}
