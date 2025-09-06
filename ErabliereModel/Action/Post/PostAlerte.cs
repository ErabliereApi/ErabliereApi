using System;
using System.ComponentModel.DataAnnotations;

namespace ErabliereApi.Action.Post;

/// <summary>
/// Modèle de création d'une alerte pour le trio de données
/// </summary>
public class PostAlerte
{
    /// <summary>
    /// L'id de l'alerte
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// L'id de l'érablière
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
    /// Si une temperature est reçu et que celle-ci est plus grande que cette valeur, cette validation sera évaluer à vrai.
    /// </summary>
    /// <example>0</example>
    [MaxLength(50)]
    public string? TemperatureThresholdLow { get; set; }

    /// <summary>
    /// Pourrait être interprété comme TemperatureMinValue
    /// </summary>
    [MaxLength(50)]
    public string? TemperatureThresholdHight { get; set; }

    /// <summary>
    /// Pourrait être interprété comme VacciumMaxValue
    /// </summary>
    [MaxLength(50)]
    public string? VacciumThresholdLow { get; set; }

    /// <summary>
    /// Si un vaccium est reçu et que celui-ci est plus petit que cette valeur, cette validation sera évaluer à vrai.
    /// </summary>
    /// <example>200</example>
    [MaxLength(50)]
    public string? VacciumThresholdHight { get; set; }

    /// <summary>
    /// Pourrait être interprété comme NiveauBassinMaxValue
    /// </summary>
    [MaxLength(50)]
    public string? NiveauBassinThresholdLow { get; set; }

    /// <summary>
    /// Pourrait être interprété comme NiveauBassinMinValue
    /// </summary>
    [MaxLength(50)]
    public string? NiveauBassinThresholdHight { get; set; }

    /// <summary>
    /// Indique si l'alerte est activé
    /// </summary>
    public bool IsEnable { get; set; }
}