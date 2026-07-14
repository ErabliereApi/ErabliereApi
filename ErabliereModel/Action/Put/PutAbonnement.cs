using System;
using System.ComponentModel.DataAnnotations;

namespace ErabliereApi.Donnees.Action.Put;

/// <summary>
/// Modèle de modification d'un abonnement. Ne contient volontairement aucune propriété
/// de navigation afin d'empêcher l'over-posting vers des entités liées. Le statut ne
/// peut pas être modifié directement : il change via l'annulation ou les webhooks Stripe.
/// </summary>
public class PutAbonnement
{
    /// <summary>
    /// L'id de l'abonnement à modifier
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Le forfait de l'abonnement. Voir <see cref="ForfaitsAbonnement" /> pour les valeurs permises.
    /// Null pour conserver le forfait actuel.
    /// </summary>
    [MaxLength(50, ErrorMessage = "Le forfait ne peut pas dépasser 50 caractères.")]
    public string? Plan { get; set; }

    /// <summary>
    /// La date de début de l'abonnement. Null pour conserver la date actuelle.
    /// </summary>
    public DateTimeOffset? DateDebut { get; set; }

    /// <summary>
    /// La date de fin de l'abonnement. Null pour conserver la date actuelle.
    /// </summary>
    public DateTimeOffset? DateFin { get; set; }
}
