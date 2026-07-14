using Stripe;

namespace ErabliereApi.Services.Abonnements;

/// <summary>
/// Service de synchronisation des abonnements locaux avec le fournisseur de paiement
/// </summary>
public interface IAbonnementService
{
    /// <summary>
    /// Active l'abonnement local de l'utilisateur suite à la création d'un abonnement Stripe.
    /// Si un abonnement en attente existe, il est activé. Sinon, un nouvel abonnement actif est créé.
    /// </summary>
    Task ActiverAbonnementStripeAsync(Donnees.Customer customer, Subscription subscription, CancellationToken token);

    /// <summary>
    /// Synchronise le statut de l'abonnement local avec le statut de l'abonnement Stripe
    /// (annulation, expiration, réactivation).
    /// </summary>
    Task SynchroniserStatutStripeAsync(Subscription subscription, CancellationToken token);
}
