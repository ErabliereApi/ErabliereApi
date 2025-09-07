using ErabliereApi.Donnees;
using ErabliereApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErabliereApi.Depot.Sql.EntityConfiguration;

/// <summary>
/// Configuration de l'entité <see cref="IpInfo"/>
/// </summary>
public class IpInfoEntityConfiguration : IEntityTypeConfiguration<IpInfo>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<IpInfo> builder)
    {
        builder.HasIndex(e => e.Ip);
    }
}
