using System;
using System.ComponentModel.DataAnnotations;

namespace ErabliereApi.Donnees;

/// <summary>
/// Nom des host de l'appareil
/// </summary>
public class NomHostAppareil
{
    /// <summary>
    /// Identifiant unique du nom d'hôte
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Nom d'hôte
    /// </summary>
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Type de l'hôte
    /// </summary>
    [MaxLength(100)]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Identifiant de l'appareil
    /// </summary>
    public Guid IdAppareil { get; set; }
}