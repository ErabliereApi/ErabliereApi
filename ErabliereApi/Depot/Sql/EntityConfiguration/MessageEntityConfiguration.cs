using ErabliereApi.Donnees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErabliereApi.Depot.Sql.EntityConfiguration;

/// <summary>
/// Configuration de l'entité Message
/// </summary>
public class MessageEntityConfiguration : IEntityTypeConfiguration<Message>
{
    /// <summary>
    /// Longueur maximale du type d'un message, voir
    /// <see cref="Donnees.Contantes.TypesMessage" />.
    /// </summary>
    public const int LongueurMaxMessageType = 32;

    /// <summary>
    /// Longueur maximale du nom d'un outil, par exemple 'get_donnees_capteur'.
    /// </summary>
    public const int LongueurMaxToolName = 64;

    /// <summary>
    /// Longueur maximale de l'identifiant d'appel d'outil produit par le
    /// fournisseur du modèle.
    /// </summary>
    public const int LongueurMaxToolCallId = 128;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        // Le contenu reste sans borne : c'est du texte de conversation, et pour un
        // message d'outil l'enveloppe JSON complète. Les trois colonnes ci-dessous
        // sont au contraire de courtes étiquettes, et nvarchar(max) leur interdirait
        // toute indexation pour rien.
        builder.Property(m => m.MessageType).HasMaxLength(LongueurMaxMessageType);

        builder.Property(m => m.ToolName).HasMaxLength(LongueurMaxToolName);

        builder.Property(m => m.ToolCallId).HasMaxLength(LongueurMaxToolCallId);
    }
}
