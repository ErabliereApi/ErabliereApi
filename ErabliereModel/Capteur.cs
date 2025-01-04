using ErabliereApi.Donnees.Interfaces;
using ErabliereApi.Donnees.Ownable;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ErabliereApi.Donnees;

/// <summary>
/// Représente un capteur
/// </summary>
public class Capteur : IIdentifiable<Guid?, Capteur>, IErabliereOwnable, ILocalizable, IDatesInfo
{
    /// <summary>
    /// L'id de l'occurence
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Id de l'érablière relier a cette donnée
    /// </summary>
    public Guid? IdErabliere { get; set; }

    /// <summary>
    /// L'érablière de ce capteur
    /// </summary>
    public Erabliere? Erabliere { get; set; }

    /// <summary>
    /// Les données du capteurs
    /// </summary>
    public List<DonneeCapteur> DonneesCapteur { get; set; } = new();

    /// <summary>
    /// Les alertes du capteur
    /// </summary>
    public List<AlerteCapteur> AlertesCapteur { get; set; } = new();

    /// <summary>
    /// Le nom donné au capteur
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string? Nom { get; set; }

    /// <summary>
    /// Indice de l'ordre du tri
    /// </summary>
    public int? IndiceOrdre { get; set; }

    /// <summary>
    /// Le symbole qui représente l'unité observer par le capteur.
    /// </summary>
    /// <example>
    /// "°c" pour représenter la temperature en celcius
    /// </example>
    [MaxLength(7)]
    public string? Symbole { get; set; }

    /// <summary>
    /// Byte qui représente la taille du graphique
    /// </summary>
    [Range(1, 12, ErrorMessage = "La taille du graphique doit être comprise entre 1 et 12")]
    public byte? Taille { get; set; }

    /// <summary>
    /// Byte qui représente le niveau de la batterie
    /// </summary>
    public byte? BatteryLevel { get; set; }

    /// <summary>
    /// Type du capteur
    /// </summary>
    [MaxLength(50)]
    public string? Type { get; set; }

    /// <summary>
    /// Id du capteur externe. Représente l'id du capteur dans le système externe.
    /// </summary>
    public string? ExternalId { get; set; }

    /// <summary>
    /// Date du dernier message
    /// </summary>
    public DateTimeOffset? LastMessageTime { get; set; }

    /// <summary>
    /// Indique si le capteur est en ligne
    /// </summary>
    public bool? Online { get; set; }

    /// <summary>
    /// Fréquence de rapport
    /// </summary>
    public int? ReportFrequency { get; set; }

    /// <summary>
    /// Indicateur permettant d'afficher ou non le graphique relié au capteur.
    /// </summary>
    public bool AfficherCapteurDashboard { get; set; }

    /// <summary>
    /// Indicateur permettant d'indiquer si les données sont entré depuis un interface graphique
    /// </summary>
    public bool AjouterDonneeDepuisInterface { get; set; }

    /// <summary>
    /// Type d'affichage dans les grphiques. Ex: line, bar, table
    /// </summary>
    [MaxLength(50)]
    public string? DisplayType { get; set; }

    /// <summary>
    /// Nombre de données à afficher dans les graphiques. Ex: 10, 20, 30
    /// </summary>
    public short? DisplayTop { get; set; }

    /// <summary>
    /// Date de l'ajout du capteur
    /// </summary>
    public DateTimeOffset? DC { get; set; }

    /// <inheritdoc />
    public double Latitude { get; set; }

    /// <inheritdoc />
    public double Longitude { get; set; }

    /// <summary>
    /// L'appareil relié au capteur
    /// </summary>
    public Appareil? Appareil { get; set; }

    /// <summary>
    /// Id de l'appareil
    /// </summary>
    public Guid? AppareilId { get; set; }

    /// <summary>
    /// Affichage minimal lors de l'affichage des données dans les graphiques
    /// </summary>
    public double? DisplayMin { get; set; }

    /// <summary>
    /// Affichage maximal lors de l'affichage des données dans les graphiques
    /// </summary>
    public double? DisplayMax { get; set; }

    /// <inheritdoc />
    public int CompareTo(Capteur? other)
    {
        return Nom?.CompareTo(other?.Nom) ?? 1;
    }
}
