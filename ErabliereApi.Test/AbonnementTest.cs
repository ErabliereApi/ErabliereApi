using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Services;
using ErabliereApi.Services.Abonnements;
using ErabliereApi.Test.Autofixture;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ErabliereApi.Test;

/// <summary>
/// Tests unitaires de la logique d'abonnement : validation des dates,
/// transitions de statut et synchronisation avec Stripe.
/// </summary>
public class AbonnementTest
{
    #region Validation des dates

    [Theory]
    [InlineData(null, null)]
    [InlineData("2026-01-01", null)]
    [InlineData(null, "2026-12-31")]
    [InlineData("2026-01-01", "2026-12-31")]
    public void DatesValides_PlagesCoherentes_RetourneVrai(string? debut, string? fin)
    {
        var dateDebut = debut == null ? (DateTimeOffset?)null : DateTimeOffset.Parse(debut);
        var dateFin = fin == null ? (DateTimeOffset?)null : DateTimeOffset.Parse(fin);

        Assert.True(Abonnement.DatesValides(dateDebut, dateFin));
    }

    [Theory]
    [InlineData("2026-12-31", "2026-01-01")]
    [InlineData("2026-01-01", "2026-01-01")]
    public void DatesValides_DebutApresOuEgalALaFin_RetourneFaux(string debut, string fin)
    {
        Assert.False(Abonnement.DatesValides(DateTimeOffset.Parse(debut), DateTimeOffset.Parse(fin)));
    }

    #endregion

    #region EstActif

    [Fact]
    public void EstActif_StatutActifSansDates_RetourneVrai()
    {
        var abonnement = new Abonnement { Statut = StatutAbonnement.Actif };

        Assert.True(abonnement.EstActif(DateTimeOffset.Now));
    }

    [Fact]
    public void EstActif_StatutActifDansLaPlage_RetourneVrai()
    {
        var maintenant = DateTimeOffset.Now;

        var abonnement = new Abonnement
        {
            Statut = StatutAbonnement.Actif,
            DateDebut = maintenant.AddDays(-1),
            DateFin = maintenant.AddDays(1)
        };

        Assert.True(abonnement.EstActif(maintenant));
    }

    [Fact]
    public void EstActif_AvantLaDateDeDebut_RetourneFaux()
    {
        var maintenant = DateTimeOffset.Now;

        var abonnement = new Abonnement
        {
            Statut = StatutAbonnement.Actif,
            DateDebut = maintenant.AddDays(1)
        };

        Assert.False(abonnement.EstActif(maintenant));
    }

    [Fact]
    public void EstActif_ApresLaDateDeFin_RetourneFaux()
    {
        var maintenant = DateTimeOffset.Now;

        var abonnement = new Abonnement
        {
            Statut = StatutAbonnement.Actif,
            DateFin = maintenant.AddDays(-1)
        };

        Assert.False(abonnement.EstActif(maintenant));
    }

    [Theory]
    [InlineData(StatutAbonnement.EnAttente)]
    [InlineData(StatutAbonnement.Annule)]
    [InlineData(StatutAbonnement.Expire)]
    public void EstActif_StatutNonActif_RetourneFaux(StatutAbonnement statut)
    {
        var abonnement = new Abonnement { Statut = statut };

        Assert.False(abonnement.EstActif(DateTimeOffset.Now));
    }

    #endregion

    #region Transitions de statut

    [Theory]
    [InlineData(StatutAbonnement.EnAttente, StatutAbonnement.Actif)]
    [InlineData(StatutAbonnement.EnAttente, StatutAbonnement.Annule)]
    [InlineData(StatutAbonnement.Actif, StatutAbonnement.Annule)]
    [InlineData(StatutAbonnement.Actif, StatutAbonnement.Expire)]
    public void ChangerStatut_TransitionPermise_ChangeLeStatut(StatutAbonnement depart, StatutAbonnement cible)
    {
        var abonnement = new Abonnement { Statut = depart };

        Assert.True(abonnement.PeutTransitionnerVers(cible));

        abonnement.ChangerStatut(cible);

        Assert.Equal(cible, abonnement.Statut);
        Assert.NotNull(abonnement.DM);
    }

    [Theory]
    [InlineData(StatutAbonnement.EnAttente, StatutAbonnement.Expire)]
    [InlineData(StatutAbonnement.Actif, StatutAbonnement.EnAttente)]
    [InlineData(StatutAbonnement.Annule, StatutAbonnement.Actif)]
    [InlineData(StatutAbonnement.Annule, StatutAbonnement.EnAttente)]
    [InlineData(StatutAbonnement.Expire, StatutAbonnement.Actif)]
    [InlineData(StatutAbonnement.Expire, StatutAbonnement.Annule)]
    public void ChangerStatut_TransitionInterdite_LanceUneException(StatutAbonnement depart, StatutAbonnement cible)
    {
        var abonnement = new Abonnement { Statut = depart };

        Assert.False(abonnement.PeutTransitionnerVers(cible));

        Assert.Throws<InvalidOperationException>(() => abonnement.ChangerStatut(cible));

        Assert.Equal(depart, abonnement.Statut);
    }

    [Theory]
    [InlineData(StatutAbonnement.Annule)]
    [InlineData(StatutAbonnement.Expire)]
    public void ChangerStatut_StatutTerminal_AucuneTransitionPermise(StatutAbonnement statutTerminal)
    {
        var abonnement = new Abonnement { Statut = statutTerminal };

        foreach (var cible in Enum.GetValues<StatutAbonnement>())
        {
            Assert.False(abonnement.PeutTransitionnerVers(cible));
        }
    }

    #endregion

    #region Forfaits

    [Theory]
    [InlineData("gratuit")]
    [InlineData("base")]
    [InlineData("Base")]
    public void ForfaitsAbonnement_ForfaitConnu_EstValide(string forfait)
    {
        Assert.True(ForfaitsAbonnement.EstValide(forfait));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("platine")]
    public void ForfaitsAbonnement_ForfaitInconnu_EstInvalide(string? forfait)
    {
        Assert.False(ForfaitsAbonnement.EstValide(forfait));
    }

    [Fact]
    public void ForfaitsAbonnement_SeulLeForfaitDeBaseEstPayant()
    {
        Assert.True(ForfaitsAbonnement.EstPayant(ForfaitsAbonnement.Base));
        Assert.False(ForfaitsAbonnement.EstPayant(ForfaitsAbonnement.Gratuit));
    }

    #endregion

    #region Fréquences de facturation

    [Theory]
    [InlineData("mensuelle")]
    [InlineData("annuelle")]
    [InlineData("Annuelle")]
    public void FrequencesFacturation_FrequenceConnue_EstValide(string frequence)
    {
        Assert.True(FrequencesFacturation.EstValide(frequence));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("hebdomadaire")]
    public void FrequencesFacturation_FrequenceInconnue_EstInvalide(string? frequence)
    {
        Assert.False(FrequencesFacturation.EstValide(frequence));
    }

    #endregion

    #region Synchronisation Stripe (AbonnementService)

    private static async Task<Customer> AjouterCustomerAsync(ErabliereDbContext context)
    {
        var id = Guid.NewGuid();

        var customer = new Customer
        {
            Id = id,
            Name = $"Customer {id}",
            UniqueName = $"{id}@test.com",
            Email = $"{id}@test.com"
        };

        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        return customer;
    }

    [Theory, AutoApiData]
    public async Task ActiverAbonnementStripe_AbonnementEnAttente_ActiveEtLieAStripe(ErabliereDbContext context)
    {
        var customer = await AjouterCustomerAsync(context);
        Assert.NotNull(customer.Id);

        context.Abonnements.Add(new Abonnement
        {
            CustomerId = customer.Id.Value,
            Plan = ForfaitsAbonnement.Base,
            Statut = StatutAbonnement.EnAttente,
            DC = DateTimeOffset.Now
        });
        await context.SaveChangesAsync();

        var service = new AbonnementService(context, NullLogger<AbonnementService>.Instance);

        await service.ActiverAbonnementStripeAsync(
            customer, new Stripe.Subscription { Id = "sub_test123" }, CancellationToken.None);

        var abonnement = context.Abonnements.Single(a => a.CustomerId == customer.Id);
        Assert.Equal(StatutAbonnement.Actif, abonnement.Statut);
        Assert.Equal("sub_test123", abonnement.StripeSubscriptionId);
        Assert.NotNull(abonnement.DateDebut);
    }

    [Theory, AutoApiData]
    public async Task ActiverAbonnementStripe_AucunAbonnementEnAttente_CreeUnAbonnementActif(ErabliereDbContext context)
    {
        var customer = await AjouterCustomerAsync(context);
        Assert.NotNull(customer.Id);

        var service = new AbonnementService(context, NullLogger<AbonnementService>.Instance);

        await service.ActiverAbonnementStripeAsync(
            customer, new Stripe.Subscription { Id = "sub_test456" }, CancellationToken.None);

        var abonnement = context.Abonnements.Single(a => a.CustomerId == customer.Id && a.StripeSubscriptionId == "sub_test456");
        Assert.Equal(StatutAbonnement.Actif, abonnement.Statut);
        Assert.Equal(ForfaitsAbonnement.Base, abonnement.Plan);
    }

    [Theory, AutoApiData]
    public async Task ActiverAbonnementStripe_PrixAnnuel_DeduitLaFrequenceAnnuelle(ErabliereDbContext context)
    {
        var customer = await AjouterCustomerAsync(context);
        Assert.NotNull(customer.Id);

        context.Abonnements.Add(new Abonnement
        {
            CustomerId = customer.Id.Value,
            Plan = ForfaitsAbonnement.Base,
            Statut = StatutAbonnement.EnAttente,
            DC = DateTimeOffset.Now
        });
        await context.SaveChangesAsync();

        var service = new AbonnementService(context, NullLogger<AbonnementService>.Instance);

        await service.ActiverAbonnementStripeAsync(
            customer, SubscriptionAvecIntervalle("sub_annuel1", "year"), CancellationToken.None);

        var abonnement = context.Abonnements.Single(a => a.CustomerId == customer.Id);
        Assert.Equal(FrequencesFacturation.Annuelle, abonnement.FrequenceFacturation);
    }

    [Theory, AutoApiData]
    public async Task ActiverAbonnementStripe_PrixMensuelSansAbonnementEnAttente_CreeAvecFrequenceMensuelle(ErabliereDbContext context)
    {
        var customer = await AjouterCustomerAsync(context);
        Assert.NotNull(customer.Id);

        var service = new AbonnementService(context, NullLogger<AbonnementService>.Instance);

        await service.ActiverAbonnementStripeAsync(
            customer, SubscriptionAvecIntervalle("sub_mensuel1", "month"), CancellationToken.None);

        var abonnement = context.Abonnements.Single(a => a.CustomerId == customer.Id && a.StripeSubscriptionId == "sub_mensuel1");
        Assert.Equal(FrequencesFacturation.Mensuelle, abonnement.FrequenceFacturation);
    }

    [Theory, AutoApiData]
    public async Task ActiverAbonnementStripe_FrequenceDejaChoisie_NEstPasEcrasee(ErabliereDbContext context)
    {
        var customer = await AjouterCustomerAsync(context);
        Assert.NotNull(customer.Id);

        context.Abonnements.Add(new Abonnement
        {
            CustomerId = customer.Id.Value,
            Plan = ForfaitsAbonnement.Base,
            FrequenceFacturation = FrequencesFacturation.Mensuelle,
            Statut = StatutAbonnement.EnAttente,
            DC = DateTimeOffset.Now
        });
        await context.SaveChangesAsync();

        var service = new AbonnementService(context, NullLogger<AbonnementService>.Instance);

        await service.ActiverAbonnementStripeAsync(
            customer, SubscriptionAvecIntervalle("sub_mensuel2", "year"), CancellationToken.None);

        var abonnement = context.Abonnements.Single(a => a.CustomerId == customer.Id);
        Assert.Equal(FrequencesFacturation.Mensuelle, abonnement.FrequenceFacturation);
    }

    private static Stripe.Subscription SubscriptionAvecIntervalle(string id, string interval)
    {
        return new Stripe.Subscription
        {
            Id = id,
            Items = new Stripe.StripeList<Stripe.SubscriptionItem>
            {
                Data =
                [
                    new Stripe.SubscriptionItem
                    {
                        Price = new Stripe.Price
                        {
                            Id = $"price_{id}",
                            Recurring = new Stripe.PriceRecurring { Interval = interval }
                        }
                    }
                ]
            }
        };
    }

    [Theory, AutoApiData]
    public async Task SynchroniserStatutStripe_AbonnementStripeAnnule_AnnuleLAbonnementLocal(ErabliereDbContext context)
    {
        var customer = await AjouterCustomerAsync(context);
        Assert.NotNull(customer.Id);

        context.Abonnements.Add(new Abonnement
        {
            CustomerId = customer.Id.Value,
            Plan = ForfaitsAbonnement.Base,
            Statut = StatutAbonnement.Actif,
            StripeSubscriptionId = "sub_test789",
            DC = DateTimeOffset.Now
        });
        await context.SaveChangesAsync();

        var service = new AbonnementService(context, NullLogger<AbonnementService>.Instance);

        await service.SynchroniserStatutStripeAsync(
            new Stripe.Subscription { Id = "sub_test789", Status = "canceled" }, CancellationToken.None);

        var abonnement = context.Abonnements.Single(a => a.StripeSubscriptionId == "sub_test789");
        Assert.Equal(StatutAbonnement.Annule, abonnement.Statut);
        Assert.NotNull(abonnement.DateFin);
    }

    #endregion

    #region Distinction abonnement de compte vs clé d'API (webhook)

    [Theory]
    [InlineData("price_mensuel")]
    [InlineData("price_annuel")]
    public void EstAbonnementCompte_PrixAbonnementConfigure_RetourneVrai(string priceId)
    {
        var options = new StripeOptions
        {
            BasePlanPriceId = "price_cle_api",
            AbonnementMensuelPriceId = "price_mensuel",
            AbonnementAnnuelPriceId = "price_annuel"
        };

        var subscription = SubscriptionAvecPriceId(priceId);

        Assert.True(StripeCheckoutService.EstAbonnementCompte(subscription, options));
    }

    [Fact]
    public void EstAbonnementCompte_PrixCleApi_RetourneFaux()
    {
        var options = new StripeOptions
        {
            BasePlanPriceId = "price_cle_api",
            AbonnementMensuelPriceId = "price_mensuel",
            AbonnementAnnuelPriceId = "price_annuel"
        };

        var subscription = SubscriptionAvecPriceId("price_cle_api");

        Assert.False(StripeCheckoutService.EstAbonnementCompte(subscription, options));
    }

    [Fact]
    public void EstAbonnementCompte_PrixAbonnementNonConfigures_RetourneFaux()
    {
        var options = new StripeOptions
        {
            BasePlanPriceId = "price_cle_api"
        };

        var subscription = SubscriptionAvecPriceId("price_cle_api");

        Assert.False(StripeCheckoutService.EstAbonnementCompte(subscription, options));
    }

    private static Stripe.Subscription SubscriptionAvecPriceId(string priceId)
    {
        return new Stripe.Subscription
        {
            Id = "sub_test",
            Items = new Stripe.StripeList<Stripe.SubscriptionItem>
            {
                Data =
                [
                    new Stripe.SubscriptionItem
                    {
                        Price = new Stripe.Price { Id = priceId }
                    }
                ]
            }
        };
    }

    #endregion
}
