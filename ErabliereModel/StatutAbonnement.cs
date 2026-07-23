using System.Text.Json.Serialization;

namespace ErabliereApi.Donnees;

/// <summary>
/// Les statuts possibles d'un <see cref="Abonnement" />
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StatutAbonnement
{
    /// <summary>
    /// L'abonnement est créé, mais en attente d'une confirmation
    /// (par exemple la complétion d'une session de paiement Stripe).
    /// </summary>
    EnAttente = 0,

    /// <summary>
    /// L'abonnement est actif.
    /// </summary>
    Actif = 1,

    /// <summary>
    /// L'abonnement a été annulé par l'utilisateur ou par le fournisseur de paiement.
    /// </summary>
    Annule = 2,

    /// <summary>
    /// L'abonnement est arrivé à échéance sans être renouvelé.
    /// </summary>
    Expire = 3
}
