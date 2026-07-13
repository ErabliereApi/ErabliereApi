using System;
using System.ComponentModel.DataAnnotations;

namespace ErabliereApi.Donnees.Action.Put;

/// <summary>
/// Modèle de modification d'une alerte (trio de données). Ne contient volontairement aucune
/// propriété de navigation afin d'empêcher l'over-posting vers des entités liées.
/// </summary>
public class PutAlerte
{
    /// <summary>
    /// L'id de l'alerte à modifier
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// La clé étrangère de l'érablière
    /// </summary>
    public Guid? IdErabliere { get; set; }

    /// <summary>
    /// Le nom de l'alerte
    /// </summary>
    [MaxLength(100)]
    public string? Nom { get; set; }

    /// <summary>
    /// Une liste d'adresse email séparer par des ';'
    /// </summary>
    [MaxLength(200)]
    public string? EnvoyerA { get; set; }

    /// <summary>
    /// Une liste de numéros de téléphone séparés par des ';'
    /// </summary>
    [MaxLength(200)]
    public string? TexterA { get; set; }

    /// <summary>
    /// Seuil de température bas
    /// </summary>
    [MaxLength(50)]
    public string? TemperatureThresholdLow { get; set; }

    /// <summary>
    /// Seuil de température haut
    /// </summary>
    [MaxLength(50)]
    public string? TemperatureThresholdHight { get; set; }

    /// <summary>
    /// Seuil de vacuum bas
    /// </summary>
    [MaxLength(50)]
    public string? VacciumThresholdLow { get; set; }

    /// <summary>
    /// Seuil de vacuum haut
    /// </summary>
    [MaxLength(50)]
    public string? VacciumThresholdHight { get; set; }

    /// <summary>
    /// Seuil de niveau de bassin bas
    /// </summary>
    [MaxLength(50)]
    public string? NiveauBassinThresholdLow { get; set; }

    /// <summary>
    /// Seuil de niveau de bassin haut
    /// </summary>
    [MaxLength(50)]
    public string? NiveauBassinThresholdHight { get; set; }

    /// <summary>
    /// Indique si l'alerte est activé
    /// </summary>
    public bool IsEnable { get; set; }
}
