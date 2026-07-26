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

    /// <summary>
    /// Retrouve l'abonnement actif du client au moment reçu en paramètre, ou null
    /// s'il n'en possède aucun.
    ///
    /// C'est l'unique source de vérité du forfait courant d'un utilisateur : les
    /// fonctionnalités restreintes par forfait (<see cref="Attributes.ValiderAbonnementAttribute" />,
    /// le serveur MCP, ...) passent toutes par ici plutôt que de réécrire la requête.
    /// </summary>
    /// <param name="customerId">L'id du <see cref="Donnees.Customer" /> concerné</param>
    /// <param name="maintenant">Le moment auquel l'abonnement doit être actif</param>
    /// <param name="token">Le token d'annulation</param>
    Task<Donnees.Abonnement?> ObtenirAbonnementCourantAsync(Guid customerId, DateTimeOffset maintenant, CancellationToken token);

    /// <summary>
    /// Retourne le forfait courant du client : celui de son abonnement actif, ou
    /// <see cref="Donnees.ForfaitsAbonnement.Gratuit" /> lorsqu'il n'en possède aucun.
    /// Un utilisateur sans abonnement n'est donc pas un cas particulier pour les
    /// appelants : il est simplement sur le forfait gratuit.
    /// </summary>
    /// <param name="customerId">L'id du <see cref="Donnees.Customer" /> concerné</param>
    /// <param name="maintenant">Le moment auquel l'abonnement doit être actif</param>
    /// <param name="token">Le token d'annulation</param>
    Task<string> ObtenirPlanCourantAsync(Guid customerId, DateTimeOffset maintenant, CancellationToken token);
}
