using ErabliereApi.Donnees.Interfaces;
using ErabliereApi.Donnees.Ownable;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace ErabliereApi.Donnees;

/// <summary>
/// Modèle représentant une érablière
/// </summary>
public class Erabliere : IIdentifiable<Guid?, Erabliere>, IUserOwnable, ILocalizable, IDatesInfo, IIsPublic, IAltitude
{
    /// <summary>
    /// L'id de l'érablière
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Le nom de l'érablière
    /// </summary>
    [Required(ErrorMessage = "Le nom de l'érablière est requis.")]
    [MaxLength(100, ErrorMessage = "Le nom de l'érablière ne peut pas dépasser 100 caractères.")]
    public string? Nom { get; set; }

    /// <summary>
    /// La description de l'érablière
    /// </summary>
    [MaxLength(500, ErrorMessage = "La description de l'érablière ne peut pas dépasser 500 caractères.")]
    public string? Description { get; set; }

    /// <summary>
    /// L'adresse de l'érablière
    /// </summary>
    [MaxLength(200, ErrorMessage = "L'adresse de l'érablière ne peut pas dépasser 200 caractères.")]
    public string? Addresse { get; set; }

    /// <summary>
    /// La région administrative de l'érablière
    /// </summary>
    [MaxLength(100, ErrorMessage = "La région administrative de l'érablière ne peut pas dépasser 100 caractères.")]
    public string? RegionAdministrative { get; set; }

    /// <summary>
    /// Addresse IP alloué à faire des opération d'écriture
    /// </summary>
    [MaxLength(50, ErrorMessage = "L'adresse IP ne peut pas dépasser 50 caractères.")]
    public string? IpRule { get; set; }

    /// <summary>
    /// Un indice permettant l'affichage des érablières dans l'ordre précisé.
    /// </summary>
    public int? IndiceOrdre { get; set; }

    /// <summary>
    /// Code postal, utiliser pour les fonctions de prédiction météo
    /// </summary>
    [MaxLength(30, ErrorMessage = "Le code postal ne peut pas dépasser 30 caractères.")]
    public string? CodePostal { get; set; }

    /// <summary>
    /// Indicateur pour l'affichage du panneau de prédiction météo par jour
    /// </summary>
    public bool? AfficherPredictionMeteoJour { get; set; }

    /// <summary>
    /// Indicateur pour l'affichage du panneau de prédiction météo par heure
    /// </summary>
    public bool? AfficherPredictionMeteoHeure { get; set; }

    /// <summary>
    /// Nombre de colonne pris par le panneau image sur la grille
    /// </summary>
    [Range(1, 12, ErrorMessage = "La taille du graphique doit être comprise entre 1 et 12")]
    public byte? DimensionPanneauImage { get; set; }

    /// <summary>
    /// Indicateur permettant de déterminer si la section des barils sera utiliser par l'érablière
    /// </summary>
    public bool? AfficherSectionBaril { get; set; }

    /// <summary>
    /// Indicateur permettant de déterminer si la section des donnees sera utiliser par l'érablière
    /// </summary>
    public bool? AfficherTrioDonnees { get; set; }

    /// <summary>
    /// Indicateur permettant de déterminer si la section des donnees sera utiliser par l'érablière
    /// </summary>
    public bool? AfficherSectionDompeux { get; set; }

    /// <summary>
    /// Indique si une érablière est publique ou une authentification est requise
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// Les capteurs de l'érablière
    /// </summary>
    public List<Capteur>? Capteurs { get; set; }

    /// <summary>
    /// Les capteurs d'images de l'érablière
    /// </summary>
    public List<CapteurImage>? CapteursImage { get; set; }

    /// <summary>
    /// Les données relier à l'érablière
    /// </summary>
    public List<Donnee>? Donnees { get; set; }

    /// <summary>
    /// La liste des barils de l'érablière
    /// </summary>
    public List<Baril>? Barils { get; set; }

    /// <summary>
    /// La liste des dompeux de l'érablière
    /// </summary>
    public List<Dompeux>? Dompeux { get; set; }

    /// <summary>
    /// La liste des notes
    /// </summary>
    public List<Note>? Notes { get; set; }

    /// <summary>
    /// La liste des documentations
    /// </summary>
    public List<Documentation>? Documentations { get; set; }

    /// <summary>
    /// Liste des alertes de type trio de données relié à l'érablière
    /// </summary>
    public List<Alerte>? Alertes { get; set; }

    /// <summary>
    /// Liste des rapports de l'érablière
    /// </summary>
    public List<Rapport>? Rapports { get; set; }

    /// <summary>
    /// Liste des inspections de l'érablière
    /// </summary>
    public List<Inspection>? Inspections { get; set; }

    /// <summary>
    /// Liste de jonction entre l'utilisateurs et ses érablières
    /// </summary>
    public List<CustomerErabliere>? CustomerErablieres { get; set; }

    /// <inheritdoc />
    public double Latitude { get; set; }

    /// <inheritdoc />
    public double Longitude { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? DC { get; set; }

    /// <inheritdoc />
    public double? Base { get; set; }

    /// <inheritdoc />
    public double? Sommet { get; set; }

    /// <summary>
    /// Les horaires d'ouverture de l'érablière
    /// </summary>
    public List<Horaire>? Horaires { get; set; }

    /// <inheritdoc />
    public int CompareTo([AllowNull] Erabliere other)
    {
        if (IndiceOrdre.HasValue && other?.IndiceOrdre == null)
        {
            return -1;
        }
        else if (other?.IndiceOrdre.HasValue == true && !IndiceOrdre.HasValue)
        {
            return 1;
        }
        else if (IndiceOrdre.HasValue && other?.IndiceOrdre.HasValue == true)
        {
            return IndiceOrdre.Value.CompareTo(IndiceOrdre);
        }

        return string.Compare(Nom, other?.Nom);
    }
}
