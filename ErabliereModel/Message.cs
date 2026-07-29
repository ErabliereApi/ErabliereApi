using System;
using System.Collections.Generic;
using ErabliereApi.Donnees.Interfaces;

namespace ErabliereApi.Donnees;

/// <summary>
/// Classe représentant un message dans une conversation avec ErabliereAI
/// </summary>
public class Message : IIdentifiable<Guid?, Message>
{
    /// <summary>
    /// Clé primaire
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Clé étrangère de la conversation
    /// </summary>
    public Guid? ConversationId { get; set; }

    /// <summary>
    /// La conversation à laquelle le message appartient
    /// </summary>
    public Conversation? Conversation { get; set; }

    /// <summary>
    /// Le contenu du message
    /// </summary>
    public string Content { get; set; } = "";

    /// <summary>
    /// Message de refus
    /// </summary>
    public string? Refusal { get; set; }

    /// <summary>
    /// Uri de l'image
    /// </summary>
    public string? ImageUri { get; set; }

    /// <summary>
    /// Partie du messages et pièces jointes
    /// </summary>
    public List<MessagePart>? MessageParts { get; set; }

    /// <summary>
    /// Date de création du message
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Indique si le message a été envoyé par un utilisateur
    /// </summary>
    public bool IsUser { get; set; }

    /// <summary>
    /// Le type de message, voir <see cref="Contantes.TypesMessage" />.
    /// Nul pour les messages antérieurs aux outils, ce qui se lit comme du texte.
    /// </summary>
    public string? MessageType { get; set; }

    /// <summary>
    /// Le nom de l'outil, pour un message de type AppelOutil ou ResultatOutil.
    /// </summary>
    public string? ToolName { get; set; }

    /// <summary>
    /// L'identifiant qui relie un ResultatOutil à l'AppelOutil correspondant.
    /// C'est l'identifiant produit par le fournisseur du modèle.
    /// </summary>
    public string? ToolCallId { get; set; }

    /// <summary>
    /// Indique que la réponse a été construite à partir de données réelles lues par
    /// les outils, et non seulement des connaissances du modèle. C'est ce que
    /// l'interface signale avec une pastille sur le message.
    /// </summary>
    public bool UsedLiveData { get; set; }

    /// <summary>
    /// Compare deux messages par leur date de création
    /// </summary>
    public int CompareTo(Message? other)
    {
        if (other == null)
        {
            return 1;
        }

        return CreatedAt.CompareTo(other.CreatedAt);
    }
}
