using ErabliereApi.Donnees.Contantes;
using System;
using System.ComponentModel.DataAnnotations;

namespace ErabliereApi.Donnees.Action.Put;

/// <summary>
/// Modèle de modification d'une donnée de capteur. Ne contient volontairement aucune propriété
/// de navigation afin d'empêcher l'over-posting vers des entités liées.
/// </summary>
public class PutDonneeCapteur
{
    /// <summary>
    /// L'id de la donnée du capteur à modifier
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// L'id du capteur de la donnée
    /// </summary>
    public Guid? IdCapteur { get; set; }

    /// <summary>
    /// La valeur de la donnée
    /// </summary>
    public decimal? Valeur { get; set; }

    /// <summary>
    /// Text associé à la donnée
    /// </summary>
    [MaxLength(Specifications.DonnesCapteurTextMaxLength)]
    public string? Text { get; set; }

    /// <summary>
    /// La date de création
    /// </summary>
    public DateTimeOffset? D { get; set; }
}
