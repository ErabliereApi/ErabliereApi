using System;
using System.ComponentModel.DataAnnotations;

namespace ErabliereApi.Donnees;

/// <summary>
/// Modèle de configuration d'un server Chirpstack
/// </summary>
public class ChirpStackSrvConfig
{
    /// <summary>
    /// Clé primaire propre à ErabliereApi
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Id du tenant configuré dans le serveur Chirpstack
    /// </summary>
    [MaxLength(100)]
    public string? TenantId { get; set; }

    /// <summary>
    /// Nom du tenant configuré dans le serveur Chirpstack
    /// </summary>
    [MaxLength(100)]
    public string? TenantName { get; set; }

    /// <summary>
    /// Id d'application configuré dans le serveur Chirpstack
    /// </summary>
    [MaxLength(100)]
    public string? ApplicationId { get; set; }

    /// <summary>
    /// Nom de l'application configuré dans le serveur Chirpstack
    /// </summary>
    [MaxLength(100)]
    public string? ApplicationName { get; set; }

    /// <summary>
    /// Id de profile d'appareil configuré dans le serveur Chripstack
    /// </summary>
    [MaxLength(100)]
    public string? DeviceProfileId { get; set; }

    /// <summary>
    /// Nom de profile d'appareil configuré dans le serveur Chirpstack
    /// </summary>
    [MaxLength(100)]
    public string? DeviceProfileName { get; set; }

    /// <summary>
    /// Class LoRaWAN utilisé
    /// </summary>
    [MaxLength(100)]
    public string? DeviceClassEnabled { get; set; }

    /// <summary>
    /// Date du dernier message de ce serveur
    /// </summary>
    public DateTimeOffset? LastTimeSeen { get; set; }
}
