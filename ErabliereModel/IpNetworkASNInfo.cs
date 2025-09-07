using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ErabliereApi.Donnees.Interfaces;

namespace ErabliereApi.Models;

/// <summary>
/// Informations sur une adresse IP
/// </summary>
public class IpNetworkAsnInfo : IIdentifiable<Guid, IpNetworkAsnInfo>
{
    /// <summary>
    /// Identifiant unique de l'information IP
    /// </summary>
    public Guid Id { get; set; }

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
    [MaxLength(2)]
    public string ContinentCode { get; set; } = string.Empty;

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
    /// Indique si le réseau est permis ou bloqué
    /// </summary>
    [DefaultValue(true)]
    public bool IsAllowed { get; set; } = true;

    /// <inheritdoc />
    public int CompareTo(IpNetworkAsnInfo? other)
    {
        return Network.CompareTo(other?.Network);
    }
}