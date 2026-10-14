using ErabliereApi.Donnees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErabliereApi.Depot.Sql.EntityConfiguration;

/// <summary>
/// Configuration de l'entité <see cref="EpisodeAlerteCapteur"/>
/// </summary>
public class EpisodeAlerteCapteurEntityConfiguration : IEntityTypeConfiguration<EpisodeAlerteCapteur>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EpisodeAlerteCapteur> builder)
    {
        builder.Ignore(e => e.OwnerId);
        builder.Ignore(e => e.Owner);

        builder.HasOne(e => e.Capteur)
               .WithMany()
               .HasForeignKey(e => e.IdCapteur)
               .OnDelete(DeleteBehavior.Cascade);

        // Pas de cascade en base : le capteur supprime déjà ses alertes et ses épisodes, et SQL Server
        // refuse deux chemins de cascade. La suppression d'une alerte détache ses épisodes elle-même
        // (AlerteCapteursController) pour conserver l'historique.
        builder.HasOne(e => e.AlerteCapteur)
               .WithMany()
               .HasForeignKey(e => e.IdAlerteCapteur)
               .OnDelete(DeleteBehavior.ClientSetNull);

        builder.HasIndex(e => new { e.IdCapteur, e.DateDebut });
        builder.HasIndex(e => new { e.IdAlerteCapteur, e.DateFin });
    }
}
