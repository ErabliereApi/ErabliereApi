using ErabliereApi.Donnees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErabliereApi.Depot.Sql.EntityConfiguration;

/// <summary>
/// Configuration de l'entité <see cref="AlerteCapteur"/>
/// </summary>
public class AlerteCapteurEntityConfiguration : IEntityTypeConfiguration<AlerteCapteur>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AlerteCapteur> builder)
    {
        builder.Ignore(e => e.OwnerId);
        builder.Ignore(e => e.Owner);

        // Deux requêtes simultanées ne peuvent pas appliquer le même changement d'état :
        // la seconde échoue et n'envoie pas de message.
        builder.Property(e => e.DateChangementEtat).IsConcurrencyToken();
    }
}
