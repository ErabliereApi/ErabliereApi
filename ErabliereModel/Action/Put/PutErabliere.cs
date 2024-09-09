using System;
using System.ComponentModel.DataAnnotations;

namespace ErabliereApi.Donnees.Action.Put;

/// <summary>
/// Modèle de modification d'une erablière
/// </summary>
public class PutErabliere
{
    /// <summary>
    /// L'id de l'érablière à modifier.
    /// </summary>
    [Required]
    public Guid? Id { get; set; }

    /// <summary>
    /// Le nouveau nom de l'érablière, si le nom est modifié
    /// </summary>
    [MaxLength(100, ErrorMessage = "Le nom de l'érablière ne peut pas dépasser 100 caractères.")]
    public string? Nom { get; set; }

    /// <summary>
    /// Spécifie les ip qui peuvent créer des opérations d'alimentation pour cette érablière. Doivent être séparé par des ';'
    /// </summary>
    [MaxLength(50, ErrorMessage = "L'adresse IP ne peut pas dépasser 50 caractères.")]
    public string? IpRule { get; set; }

    /// <summary>
    /// Un indice permettant l'affichage des érablières dans l'ordre précisé.
    /// </summary>
    public int? IndiceOrdre { get; set; }

    /// <summary>
    /// Code postal utilisé pour les prédictions météo
    /// </summary>
    [MaxLength(30, ErrorMessage = "Le code postal ne peut pas dépasser 30 caractères.")]
    public string? CodePostal { get; set; }

    /// <summary>
    /// Indicateur permettant de déterminer si la section des prédictions météo sera utiliser par l'érablière
    /// </summary>
    public bool? AfficherPredictionMeteoJour { get; set; }

    /// <summary>
    /// Indicateur permettant de déterminer si la section des prédictions météo sera utiliser par l'érablière
    /// </summary>
    public bool? AfficherPredictionMeteoHeure { get; set; }

    /// <summary>
    /// Nombre de colonnes utiliser par le panneau d'image
    /// </summary>
    [Range(1, 12, ErrorMessage = "Le nombre de colonne doit être entre 1 et 12")]
    public int? DimensionPanneauImage { get; set; }

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
    /// Indiuateur permettant une accès en lecture à l'érablière sans authentifications
    /// </summary>
    public bool? IsPublic { get; set; }
}
