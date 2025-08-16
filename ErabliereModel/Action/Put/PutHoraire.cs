using System;
using System.ComponentModel.DataAnnotations;

namespace ErabliereApi.Donnees.Action.Put;

/// <summary>
/// Modèle d'ajout et modification d'horaire
/// </summary>
public class PutHoraire
{
    /// <summary>
    /// Id de l'érablière
    /// </summary>
    public Guid IdErabliere { get; set; }

    /// <summary>
    /// Horaire du lundi
    /// </summary>
    [MaxLength(12)]
    [RegularExpression(@"^\d{2}:\d{2}-\d{2}:\d{2}$", ErrorMessage = "Le format doit être HH:mm-HH:mm")]
    public string? Lundi { get; set; }

    /// <summary>
    /// Horaire du mardi
    /// </summary>
    [MaxLength(12)]
    [RegularExpression(@"^\d{2}:\d{2}-\d{2}:\d{2}$", ErrorMessage = "Le format doit être HH:mm-HH:mm")]
    public string? Mardi { get; set; }

    /// <summary>
    /// Horaire du mercredi
    /// </summary>
    [MaxLength(12)]
    [RegularExpression(@"^\d{2}:\d{2}-\d{2}:\d{2}$", ErrorMessage = "Le format doit être HH:mm-HH:mm")]
    public string? Mercredi { get; set; }

    /// <summary>
    /// Horaire du jeudi
    /// </summary>
    [MaxLength(12)]
    [RegularExpression(@"^\d{2}:\d{2}-\d{2}:\d{2}$", ErrorMessage = "Le format doit être HH:mm-HH:mm")]
    public string? Jeudi { get; set; }

    /// <summary>
    /// Horaire du vendredi
    /// </summary>
    [MaxLength(12)]
    [RegularExpression(@"^\d{2}:\d{2}-\d{2}:\d{2}$", ErrorMessage = "Le format doit être HH:mm-HH:mm")]
    public string? Vendredi { get; set; }

    /// <summary>
    /// Horaire du samedi
    /// </summary>
    [MaxLength(12)]
    [RegularExpression(@"^\d{2}:\d{2}-\d{2}:\d{2}$", ErrorMessage = "Le format doit être HH:mm-HH:mm")]
    public string? Samedi { get; set; }

    /// <summary>
    /// Horaire du dimanche
    /// </summary>
    [MaxLength(12)]
    [RegularExpression(@"^\d{2}:\d{2}-\d{2}:\d{2}$", ErrorMessage = "Le format doit être HH:mm-HH:mm")]
    public string? Dimanche { get; set; }
}