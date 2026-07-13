using System;
using System.ComponentModel.DataAnnotations;

namespace ErabliereApi.Donnees.Action.Post;

/// <summary>
/// Modèle de création d'une alerte de capteur. Ne contient volontairement aucune propriété de
/// navigation afin d'empêcher l'over-posting vers des entités liées.
/// </summary>
public class PostAlerteCapteur
{
    /// <summary>
    /// L'id guid si le client désire initialiser l'id
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// L'id du capteur
    /// </summary>
    [Required]
    public Guid? IdCapteur { get; set; }

    /// <summary>
    /// Le nom de l'alerte
    /// </summary>
    [MaxLength(100)]
    public string? Nom { get; set; }

    /// <summary>
    /// Une liste d'adresse email séparer par des ';'
    /// </summary>
    /// <example>exemple@courriel.com;exemple2@courriel.com</example>
    [MaxLength(200)]
    public string? EnvoyerA { get; set; }

    /// <summary>
    /// Une liste de numéros de téléphone séparés par des ';'
    /// </summary>
    /// <example>+14375327599;+15749375019</example>
    [MaxLength(200)]
    public string? TexterA { get; set; }

    /// <summary>
    /// Date de création
    /// </summary>
    public DateTime? DC { get; set; }

    /// <summary>
    /// La valeur minimal de ce capteur
    /// </summary>
    public decimal? MinValue { get; set; }

    /// <summary>
    /// La valeur maximal de ce capteur
    /// </summary>
    public decimal? MaxValue { get; set; }

    /// <summary>
    /// Indique si l'alerte est activé
    /// </summary>
    public bool IsEnable { get; set; }
}
