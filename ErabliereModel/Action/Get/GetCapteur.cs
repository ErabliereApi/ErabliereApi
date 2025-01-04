using System;
using System.ComponentModel.DataAnnotations;

namespace ErabliereApi.Donnees.Action.Get;

/// <summary>
/// Modèle de retour de l'action d'obtention d'un capteur
/// </summary>
public class GetCapteur
{
    /// <summary>
    /// L'id du catpeur
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// L'id de l'érablière
    /// </summary>
    public Guid? IdErabliere { get; set; }

    /// <summary>
    /// Le nom du capteur
    /// </summary>
    public string? Nom { get; set; }

    /// <summary>
    /// Le symbole du capteur
    /// </summary>
    public string? Symbole { get; set; }

    /// <summary>
    /// Indicateur permettant d'afficher ou non le graphique relié au capteur.
    /// </summary>
    public bool? AfficherCapteurDashboard { get; set; }

    /// <summary>
    /// Indicateur si les données sont ajouter depuis un interface graphique
    /// </summary>
    public bool AjouterDonneeDepuisInterface { get; set; }

    /// <summary>
    /// La date de création de l'entité.
    /// </summary>
    public DateTimeOffset? DC { get; set; }

    /// <summary>
    /// L'indice du tri
    /// </summary>
    public int? IndiceOrdre { get; set; }

    /// <summary>
    /// la string bootstrap pour chnager les dimensions du graphique
    /// </summary>
    public string? Taille { get; set; }

    /// <summary>
    /// Le niveau de la batterie
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
    /// Type d'affichage
    /// </summary>
    public string? DisplayType { get; set; }

    /// <summary>
    /// Nombre d'élément affiché par défaut
    /// </summary>
    public short? DisplayTop { get; set; }

    /// <summary>
    /// Affichage minimal lors de l'affichage des données dans les graphiques
    /// </summary>
    public double? DisplayMin { get; set; }

    /// <summary>
    /// Affichage maximal lors de l'affichage des données dans les graphiques
    /// </summary>
    public double? DisplayMax { get; set; }
}
