using System;
using System.Collections.Generic;
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

    /// <summary>
    /// DevEui du dernier appareil vue
    /// </summary>
    [MaxLength(100)]
    public string? LastDeviceSeen { get; set; }

    /// <summary>
    /// Nombre de message à conserver dans l'historique
    /// </summary>
    public int KeepNLastMessage { get; set; } = 100;

    /// <summary>
    /// Temps de conservation des messages dans l'historique
    /// </summary>
    public TimeSpan? TimeToKeepLastMessage { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Historique des messages reçu
    /// </summary>
    public List<ChirpStackMessage>? ChirpStackMessagesHistory { get; set; }
}

/// <summary>
/// Message reçu de chirpstack
/// </summary>
public class ChirpStackMessage
{
    /// <summary>
    /// Clé primaire
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Id du serveur
    /// </summary>
    public Guid? ChirpStackSrvConfigId { get; set; }

    /// <summary>
    /// Message Json reçu
    /// </summary>
    public string? MessageJson { get; set; }

    /// <summary>
    /// Date du message
    /// </summary>
    public DateTimeOffset? Date { get; set; }

    /// <summary>
    /// Data LoRaWAN
    /// </summary>
    [MaxLength(255)]
    public string? Data { get; set; }

    /// <summary>
    /// Données décodé par ErabliereApi
    /// </summary>
    public string? DecodedData { get; set; }
}