using System;
using System.ComponentModel.DataAnnotations;
using ErabliereApi.Donnees.Interfaces;
using ErabliereApi.Donnees.Ownable;

namespace ErabliereApi.Donnees;

/// <summary>
/// Représente les horaires d'ouverture.
/// </summary>
public class Horaire : IIdentifiable<Guid, Horaire>, IErabliereOwnable
{
    /// <summary>
    /// Clé primaire de l'horaire
    /// </summary>
    public Guid Id { get; set; }

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

    /// <inheritdoc />
    public Guid? IdErabliere { get; set; }

    /// <inheritdoc />
    public Erabliere? Erabliere { get; set; }

    /// <inheritdoc />
    public int CompareTo(Horaire? other)
    {
        return 0;
    }
}