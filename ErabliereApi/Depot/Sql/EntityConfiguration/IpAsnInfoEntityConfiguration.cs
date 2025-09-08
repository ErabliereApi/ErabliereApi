using ErabliereApi.Donnees;
using ErabliereApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ErabliereApi.Depot.Sql.EntityConfiguration;

/// <summary>
/// Configuration de l'entité <see cref="IpNetworkAsnInfo"/>
/// </summary>
public class IpNetworkAsnInfoEntityConfiguration : IEntityTypeConfiguration<IpNetworkAsnInfo>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<IpNetworkAsnInfo> builder)
    {
        builder.HasIndex(e => e.Network);
        builder.HasIndex(e => e.ASN);
        builder.HasIndex(e => e.CountryCode);
    }
}
