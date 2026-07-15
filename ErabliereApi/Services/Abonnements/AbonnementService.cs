using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace ErabliereApi.Services.Abonnements;

/// <summary>
/// Implémentation de <see cref="IAbonnementService" /> basée sur la table Abonnements
/// </summary>
public class AbonnementService : IAbonnementService
{
    private readonly ErabliereDbContext _depot;
    private readonly ILogger<AbonnementService> _logger;

    /// <summary>
    /// Constructeur par initialisation
    /// </summary>
    public AbonnementService(ErabliereDbContext depot, ILogger<AbonnementService> logger)
    {
        _depot = depot;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task ActiverAbonnementStripeAsync(Donnees.Customer customer, Subscription subscription, CancellationToken token)
    {
        if (customer.Id == null)
        {
            throw new InvalidOperationException("Le customer doit posséder un id pour activer un abonnement.");
        }

        var abonnement = await _depot.Abonnements
            .Where(a => a.CustomerId == customer.Id && a.Statut == StatutAbonnement.EnAttente)
            .OrderByDescending(a => a.DC)
            .FirstOrDefaultAsync(token);

        if (abonnement != null)
        {
            abonnement.ChangerStatut(StatutAbonnement.Actif);
            abonnement.StripeSubscriptionId = subscription.Id;
            abonnement.DateDebut ??= DateTimeOffset.Now;
            abonnement.FrequenceFacturation ??= FrequenceDepuisStripe(subscription);
        }
        else
        {
            await _depot.Abonnements.AddAsync(new Abonnement
            {
                CustomerId = customer.Id.Value,
                Plan = ForfaitsAbonnement.Base,
                FrequenceFacturation = FrequenceDepuisStripe(subscription),
                Statut = StatutAbonnement.Actif,
                DateDebut = DateTimeOffset.Now,
                StripeSubscriptionId = subscription.Id,
                DC = DateTimeOffset.Now,
                DM = DateTimeOffset.Now
            }, token);
        }

        await _depot.SaveChangesAsync(token);
    }

    /// <summary>
    /// Déduit la fréquence de facturation locale de l'intervalle de récurrence
    /// du prix de l'abonnement Stripe.
    /// </summary>
    private static string? FrequenceDepuisStripe(Subscription subscription)
    {
        var interval = subscription.Items?.Data?
            .Select(i => i.Price?.Recurring?.Interval)
            .FirstOrDefault(i => i != null);

        return interval switch
        {
            "month" => FrequencesFacturation.Mensuelle,
            "year" => FrequencesFacturation.Annuelle,
            _ => null
        };
    }

    /// <inheritdoc />
    public async Task SynchroniserStatutStripeAsync(Subscription subscription, CancellationToken token)
    {
        var abonnement = await _depot.Abonnements
            .FirstOrDefaultAsync(a => a.StripeSubscriptionId == subscription.Id, token);

        if (abonnement == null)
        {
            _logger.LogWarning("Aucun abonnement local relié à l'abonnement Stripe {SubscriptionId}", subscription.Id);
            return;
        }

        var statutCible = subscription.Status switch
        {
            "canceled" => StatutAbonnement.Annule,
            "incomplete_expired" or "unpaid" => StatutAbonnement.Expire,
            "active" or "trialing" => StatutAbonnement.Actif,
            _ => abonnement.Statut
        };

        if (statutCible == abonnement.Statut)
        {
            return;
        }

        if (!abonnement.PeutTransitionnerVers(statutCible))
        {
            _logger.LogWarning(
                "Transition de statut {StatutActuel} vers {StatutCible} refusée pour l'abonnement {AbonnementId}",
                abonnement.Statut, statutCible, abonnement.Id);
            return;
        }

        abonnement.ChangerStatut(statutCible);

        if (statutCible is StatutAbonnement.Annule or StatutAbonnement.Expire)
        {
            abonnement.DateFin ??= DateTimeOffset.Now;
        }

        await _depot.SaveChangesAsync(token);
    }
}
