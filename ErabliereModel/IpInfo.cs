using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ErabliereApi.Donnees.Interfaces;

namespace ErabliereApi.Models;

/// <summary>
/// Informations sur une adresse IP
/// </summary>
public class IpInfo : IIdentifiable<Guid, IpInfo>, ILocalizable, IDatesInfo
{
    /// <summary>
    /// Identifiant unique de l'information IP
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Adresse IP
    /// </summary>
    [MaxLength(45)]
    public string Ip { get; set; } = string.Empty;

    /// <summary>
    /// Réseau de l'adresse IP
    /// </summary>
    [MaxLength(45)]
    public string Network { get; set; } = string.Empty;

    /// <summary>
    /// Pays de l'adresse IP
    /// </summary>
    [MaxLength(100)]
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// Code du pays de l'adresse IP (ISO 3166-1 alpha-2)
    /// </summary>
    [MaxLength(2)]
    public string CountryCode { get; set; } = string.Empty;

    /// <summary>
    /// Continent de l'adresse IP
    /// </summary>
    [MaxLength(50)]
    public string Continent { get; set; } = string.Empty;

    /// <summary>
    /// Code du continent de l'adresse IP
    /// </summary>
    [MaxLength(25)]
    public string ASN { get; set; } = string.Empty;

    /// <summary>
    /// Nom de l'ASN
    /// </summary>
    [MaxLength(200)]
    public string AS_Name { get; set; } = string.Empty;

    /// <summary>
    /// Domaine de l'ASN
    /// </summary>
    [MaxLength(200)]
    public string AS_Domain { get; set; } = string.Empty;

    /// <summary>
    /// Indique si l'adresse IP est autorisée à accéder à l'application
    /// </summary>
    [DefaultValue(true)]
    public bool IsAllowed { get; set; } = true;

    /// <inheritdoc />
    public double Latitude { get; set; }

    /// <inheritdoc />
    public double Longitude { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? DC { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? DM { get; set; }

    /// <summary>
    /// Date d'expiration de l'information IP
    /// </summary>
    public DateTimeOffset? TTL { get; set; }

    /// <inheritdoc />
    public int CompareTo(IpInfo? other)
    {
        return Ip.CompareTo(other?.Ip);
    }
}