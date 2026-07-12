using ErabliereApi.Donnees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErabliereApi.Depot.Sql.EntityConfiguration;

/// <summary>
/// Configuration de la table <see cref="ErabliereDbContext.Entailles" />
/// </summary>
public class EntailleEntityConfiguration : IEntityTypeConfiguration<Entaille>
{
    /// <inheritdoc />
    /// <remarks>
    /// Les relations optionnelles vers l'arbre et la ligne de tubelure utilisent
    /// ClientSetNull pour éviter les chemins de cascade multiples refusés par SQL Server
    /// (Erabliere -> Entaille en cascade et Erabliere -> Arbre -> Entaille). Les contrôleurs
    /// doivent mettre à null les clés étrangères des entailles dépendantes avant de
    /// supprimer un arbre ou une ligne.
    /// </remarks>
    public void Configure(EntityTypeBuilder<Entaille> entaille)
    {
        entaille.HasOne(e => e.Arbre)
                .WithMany(a => a.Entailles)
                .HasForeignKey(e => e.IdArbre)
                .OnDelete(DeleteBehavior.ClientSetNull);

        entaille.HasOne(e => e.LigneTubelure)
                .WithMany(l => l.Entailles)
                .HasForeignKey(e => e.IdLigneTubelure)
                .OnDelete(DeleteBehavior.ClientSetNull);
    }
}
