using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ErabliereApi.Donnees;

/// <summary>
/// Service offert par un port d'un appareil
/// </summary>
public class PortService
{
    /// <summary>
    /// Identifiant unique du service
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Nom du service (ex: http, ftp, ssh, etc.)
    /// </summary>
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description du service
    /// </summary>
    [MaxLength(500)]
    public string? Produit { get; set; }

    /// <summary>
    /// Version du service
    /// </summary>
    public string? ExtraInfo { get; set; }

    /// <summary>
    /// Méthode de détection du service (ex: banner, fingerprint, etc.)
    /// </summary>
    [MaxLength(100)]
    public string? Methode { get; set; }

    /// <summary>
    /// Liste des CPE (Common Platform Enumeration) associés au service
    /// </summary>
    public List<string>? CPEs { get; set; }

    /// <summary>
    /// Identifiant de l'état du port
    /// </summary>
    public Guid IdPortEtat { get; set; }
}