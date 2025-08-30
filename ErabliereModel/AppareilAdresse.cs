using System;

namespace ErabliereApi.Donnees;

/// <summary>
/// Représente une adresse d'appareil.
/// </summary>
public class AppareilAdresse
{
    /// <summary>
    /// Identifiant unique de l'adresse.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Adresse de l'appareil.
    /// </summary>
    public string Addr { get; set; } = string.Empty;

    /// <summary>
    /// Type d'adresse (MAC, IPv4, IPv6, etc.).
    /// </summary>
    public string Addrtype { get; set; } = string.Empty;

    /// <summary>
    /// Fabricant.
    /// </summary>
    public string Vendeur { get; set; } = string.Empty;

    /// <summary>
    /// Identifiant unique de l'appareil.
    /// </summary>
    public Guid IdAppareil { get; set; }
}