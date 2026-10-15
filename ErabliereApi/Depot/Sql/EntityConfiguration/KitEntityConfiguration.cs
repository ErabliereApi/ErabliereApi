using ErabliereApi.Donnees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErabliereApi.Depot.Sql.EntityConfiguration;

/// <summary>
/// Configuration de la table des kits
/// </summary>
public class KitEntityConfiguration : IEntityTypeConfiguration<Kit>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Kit> builder)
    {
        // Le kit disparaît avec son érablière. Les capteurs et la clé d'API ne sont pas des
        // clés étrangères : SQL Server refuse plusieurs chemins de suppression en cascade.
        builder.HasOne(k => k.Erabliere)
            .WithMany()
            .HasForeignKey(k => k.IdErabliere)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(k => k.IdApiKey);
    }
}
