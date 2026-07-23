using ErabliereApi.Donnees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErabliereApi.Depot.Sql.EntityConfiguration;

/// <summary>
/// Configuration de la table <see cref="ErabliereDbContext.Abonnements" />
/// </summary>
public class AbonnementEntityConfiguration : IEntityTypeConfiguration<Abonnement>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Abonnement> abonnement)
    {
        abonnement.HasOne(a => a.Customer)
                  .WithMany(c => c.Abonnements)
                  .HasForeignKey(a => a.CustomerId);

        abonnement.HasIndex(a => a.CustomerId);

        abonnement.HasIndex(a => a.StripeSubscriptionId);
    }
}
