using System;
using System.ComponentModel.DataAnnotations;

namespace ErabliereApi.Donnees.Action.Put;

/// <summary>
/// Modèle de modification d'un capteur
/// </summary>
public class PutCapteur
{
    /// <summary>
    /// L'id du capteur à modifier
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Le nom du capteur
    /// </summary>
    public string? Nom { get; set; }

    /// <summary>
    /// Le symbole du capteur
    /// </summary>
    public string? Symbole { get; set; }

    /// <summary>
    /// L'id de l'érablière
    /// </summary>
    public Guid? IdErabliere { get; set; }

    /// <summary>
    /// Indicateur permettant d'afficher ou non le graphique relié au capteur.
    /// </summary>
    public bool? AfficherCapteurDashboard { get; set; }

    /// <summary>
    /// Indicateur permettant d'indiquer si les données sont entré depuis un interface graphique
    /// </summary>
    public bool? AjouterDonneeDepuisInterface { get; set; }

    /// <summary>
    /// La date de création de l'entité.
    /// </summary>
    public DateTimeOffset? DC { get; set; }

    /// <summary>
    /// Indice du tri
    /// </summary>
    public int? IndiceOrdre { get; set; }

    /// <summary>
    /// Byte qui représente la taille du graphique
    /// </summary>
    [Range(1, 12, ErrorMessage = "La taille du graphique doit être comprise entre 1 et 12")]
    public byte? Taille { get; set; }

    /// <summary>
    /// Niveau de la batterie
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
    /// Le type d'affichage du capteur
    /// </summary>
    public string? DisplayType { get; set; }

    /// <summary>
    /// Le nombre de données affiché par défaut
    /// </summary>
    public short? DisplayTop { get; set; }
}
