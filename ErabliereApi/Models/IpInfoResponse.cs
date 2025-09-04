namespace ErabliereApi.Models;

/// <summary>
/// Réponse de l'API IpInfo
/// </summary>
public class IpInfoResponse
{
    /// <summary>
    /// Adresse IP
    /// </summary>
    public string Ip { get; set; } = string.Empty;

    /// <summary>
    /// Autonomous System Number (ASN)
    /// </summary>
    public string Asn { get; set; } = string.Empty;

    /// <summary>
    /// Nom de l'AS
    /// </summary>
    public string As_name { get; set; } = string.Empty;

    /// <summary>
    /// Domaine de l'AS
    /// </summary>
    public string As_domain { get; set; } = string.Empty;

    /// <summary>
    /// Code du pays (ISO 3166-1 alpha-2)
    /// </summary>
    public string Country_code { get; set; } = string.Empty;

    /// <summary>
    /// Nom du pays
    /// </summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// Code du continent (ISO 3166-1 alpha-2)
    /// </summary>
    public string Continent_code { get; set; } = string.Empty;

    /// <summary>
    /// Nom du continent
    /// </summary>
    public string Continent { get; set; } = string.Empty;

    /// <summary>
    /// Indique si l'adresse IP est une adresse bogon
    /// </summary>
    public bool Bogon { get; set; }
}