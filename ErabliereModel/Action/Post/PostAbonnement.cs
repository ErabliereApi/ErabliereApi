using System;
using System.ComponentModel.DataAnnotations;

namespace ErabliereApi.Donnees.Action.Post;

/// <summary>
/// Modèle de création d'un abonnement. Ne contient volontairement aucune propriété
/// de navigation afin d'empêcher l'over-posting vers des entités liées. L'utilisateur
/// propriétaire est déduit du jeton d'authentification, jamais du corps de la requête.
/// </summary>
public class PostAbonnement
{
    /// <summary>
    /// Le forfait de l'abonnement. Voir <see cref="ForfaitsAbonnement" /> pour les valeurs permises.
    /// </summary>
    [Required]
    [MaxLength(50, ErrorMessage = "Le forfait ne peut pas dépasser 50 caractères.")]
    public string? Plan { get; set; }

    /// <summary>
    /// La date de début de l'abonnement. Si null, la date courante est utilisée.
    /// </summary>
    public DateTimeOffset? DateDebut { get; set; }

    /// <summary>
    /// La date de fin de l'abonnement, ou null si l'abonnement n'a pas d'échéance.
    /// </summary>
    public DateTimeOffset? DateFin { get; set; }
}
