using ErabliereApi.Controllers;
using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Donnees.Action.Put;
using ErabliereApi.Services.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ErabliereApi.Test;

/// <summary>
/// L'historique des pannes : chaque changement d'état d'une alerte de capteur ouvre ou ferme un épisode.
/// Même cas d'usage que <see cref="AlerteCapteurServiceTest"/> : le capteur « alimentation secteur »
/// envoie 0 pendant la panne et 1 au retour, avec une alerte dont MinValue = 0.
/// </summary>
public class EpisodeAlerteCapteurTest
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 13, 13, 46, 0, TimeSpan.FromHours(-4));

    private readonly string _nomBD = Guid.NewGuid().ToString();
    private readonly Guid _idErabliere = Guid.NewGuid();
    private readonly Guid _idCapteur = Guid.NewGuid();
    private readonly Guid _idAlerte = Guid.NewGuid();

    private ErabliereDbContext CreerDepot()
    {
        return new ErabliereDbContext(new DbContextOptionsBuilder<ErabliereDbContext>().UseInMemoryDatabase(_nomBD).Options);
    }

    private AlerteCapteurService CreerService(ErabliereDbContext depot)
    {
        return new AlerteCapteurService(depot, new FileNotificationsAlerte(), NullLogger<AlerteCapteurService>.Instance);
    }

    private async Task<AlerteCapteurService> CreerServiceAvecAlerte(Action<AlerteCapteur>? configurer = null)
    {
        var depot = CreerDepot();

        depot.Erabliere.Add(new Erabliere { Id = _idErabliere, Nom = "Érablière du Rang 4" });
        depot.Capteurs.Add(new Capteur { Id = _idCapteur, IdErabliere = _idErabliere, Nom = "Secteur", Symbole = "V" });

        var alerte = new AlerteCapteur
        {
            Id = _idAlerte,
            IdCapteur = _idCapteur,
            Nom = "Panne de courant",
            MinValue = 0,
            IsEnable = true
        };
        configurer?.Invoke(alerte);
        depot.AlerteCapteurs.Add(alerte);

        await depot.SaveChangesAsync();

        return CreerService(CreerDepot());
    }

    private async Task<EpisodeAlerteCapteur[]> LireEpisodes()
    {
        return await CreerDepot().EpisodesAlerteCapteur.AsNoTracking().OrderBy(e => e.DateDebut).ToArrayAsync();
    }

    private static PutAlerteCapteur Put(AlerteCapteur alerte)
    {
        return new PutAlerteCapteur
        {
            Id = alerte.Id,
            IdCapteur = alerte.IdCapteur,
            Nom = alerte.Nom,
            MinValue = alerte.MinValue,
            MaxValue = alerte.MaxValue,
            Marge = alerte.Marge,
            IsEnable = alerte.IsEnable
        };
    }

    [Fact]
    public async Task Panne_OuvreUnEpisodeALaDateDeLaDonnee()
    {
        var service = await CreerServiceAvecAlerte();

        await service.EvaluerAsync(_idCapteur, 0, T0, CancellationToken.None);
        await service.EvaluerAsync(_idCapteur, 0, T0.AddMinutes(1), CancellationToken.None);

        var episode = Assert.Single(await LireEpisodes());
        Assert.Equal(_idAlerte, episode.IdAlerteCapteur);
        Assert.Equal(_idCapteur, episode.IdCapteur);
        Assert.Equal("Panne de courant", episode.NomAlerte);
        Assert.Equal(T0, episode.DateDebut);
        Assert.Equal(0, episode.ValeurDebut);
        Assert.Null(episode.DateFin);
        Assert.Null(episode.RaisonFin);
    }

    [Fact]
    public async Task RetourDuCourant_FermeLEpisodeAvecLaDonneeDeRetour()
    {
        var service = await CreerServiceAvecAlerte();

        await service.EvaluerAsync(_idCapteur, 0, T0, CancellationToken.None);
        await service.EvaluerAsync(_idCapteur, 1, T0.AddMinutes(8), CancellationToken.None);

        var episode = Assert.Single(await LireEpisodes());
        Assert.Equal(T0.AddMinutes(8), episode.DateFin);
        Assert.Equal(1, episode.ValeurFin);
        Assert.Equal(RaisonFinEpisode.RetourNormale, episode.RaisonFin);
    }

    [Fact]
    public async Task PannesSuccessives_DonnentUnEpisodeChacune()
    {
        var service = await CreerServiceAvecAlerte();

        await service.EvaluerAsync(_idCapteur, 0, T0, CancellationToken.None);
        await service.EvaluerAsync(_idCapteur, 1, T0.AddMinutes(10), CancellationToken.None);
        await service.EvaluerAsync(_idCapteur, 0, T0.AddHours(3), CancellationToken.None);

        var episodes = await LireEpisodes();
        Assert.Equal(2, episodes.Length);
        Assert.Equal(T0.AddMinutes(10), episodes[0].DateFin);
        Assert.Equal(T0.AddHours(3), episodes[1].DateDebut);
        Assert.Null(episodes[1].DateFin);
    }

    [Fact]
    public async Task DonneeEnDiffere_PlusAncienneQueLeRetour_NeRouvrePasDEpisode()
    {
        var service = await CreerServiceAvecAlerte();

        await service.EvaluerAsync(_idCapteur, 0, T0, CancellationToken.None);
        await service.EvaluerAsync(_idCapteur, 1, T0.AddMinutes(30), CancellationToken.None);
        await service.EvaluerAsync(_idCapteur, 0, T0.AddMinutes(15), CancellationToken.None);

        var episode = Assert.Single(await LireEpisodes());
        Assert.Equal(T0.AddMinutes(30), episode.DateFin);
    }

    [Fact]
    public async Task ChangementDEtatSimultane_NOuvreQuUnEpisode()
    {
        await CreerServiceAvecAlerte();

        var depot1 = CreerDepot();
        var depot2 = CreerDepot();
        await depot1.AlerteCapteurs.LoadAsync();
        await depot2.AlerteCapteurs.LoadAsync();

        await CreerService(depot1).EvaluerAsync(_idCapteur, 0, T0, CancellationToken.None);
        await CreerService(depot2).EvaluerAsync(_idCapteur, 0, T0.AddSeconds(1), CancellationToken.None);

        var episode = Assert.Single(await LireEpisodes());
        Assert.Equal(T0, episode.DateDebut);
        Assert.Empty(depot2.ChangeTracker.Entries<EpisodeAlerteCapteur>());
    }

    [Fact]
    public async Task ChangementDEtatSimultane_SurUneAlerte_NEnregistrePasLEpisodeAvecLAlerteSuivante()
    {
        await CreerServiceAvecAlerte();

        // Une deuxième alerte sur le même capteur : son SaveChanges ne doit pas emporter
        // l'épisode refusé de la première.
        var idAutre = Guid.NewGuid();
        using (var depot = CreerDepot())
        {
            depot.AlerteCapteurs.Add(new AlerteCapteur { Id = idAutre, IdCapteur = _idCapteur, Nom = "Secteur bas", MinValue = 0, IsEnable = true });
            await depot.SaveChangesAsync();
        }

        var depot1 = CreerDepot();
        var depot2 = CreerDepot();
        await depot1.AlerteCapteurs.LoadAsync();
        await depot2.AlerteCapteurs.LoadAsync();

        await CreerService(depot1).EvaluerAsync(_idCapteur, 0, T0, CancellationToken.None);
        await CreerService(depot2).EvaluerAsync(_idCapteur, 0, T0.AddSeconds(1), CancellationToken.None);

        var episodes = await LireEpisodes();
        Assert.Equal(2, episodes.Length);
        Assert.Single(episodes, e => e.IdAlerteCapteur == _idAlerte);
        Assert.Single(episodes, e => e.IdAlerteCapteur == idAutre);
    }

    [Fact]
    public async Task AlerteEnCoursSansEpisode_LaFinCreeLEpisodeDepuisLaDerniereOccurence()
    {
        // Une alerte déjà en cours avant l'historique, que la migration n'aurait pas reprise
        var service = await CreerServiceAvecAlerte(a =>
        {
            a.EstEnAlerte = true;
            a.LastOccurence = T0;
            a.DateChangementEtat = T0;
        });

        await service.EvaluerAsync(_idCapteur, 1, T0.AddMinutes(8), CancellationToken.None);

        var episode = Assert.Single(await LireEpisodes());
        Assert.Equal(T0, episode.DateDebut);
        Assert.Null(episode.ValeurDebut);
        Assert.Equal(T0.AddMinutes(8), episode.DateFin);
        Assert.Equal(RaisonFinEpisode.RetourNormale, episode.RaisonFin);
    }

    [Fact]
    public async Task ModificationDesSeuils_FermeLEpisodeSansValeur()
    {
        await (await CreerServiceAvecAlerte()).EvaluerAsync(_idCapteur, 0, T0, CancellationToken.None);

        var depot = CreerDepot();
        var alerte = await depot.AlerteCapteurs.AsNoTracking().SingleAsync();
        var put = Put(alerte);
        put.Marge = 1;

        await new AlerteCapteursController(depot).Modifier(_idCapteur, put, CancellationToken.None);

        var episode = Assert.Single(await LireEpisodes());
        Assert.NotNull(episode.DateFin);
        Assert.Null(episode.ValeurFin);
        Assert.Equal(RaisonFinEpisode.AlerteModifiee, episode.RaisonFin);
    }

    [Fact]
    public async Task ModificationDuNom_GardeLEpisodeOuvert()
    {
        await (await CreerServiceAvecAlerte()).EvaluerAsync(_idCapteur, 0, T0, CancellationToken.None);

        var depot = CreerDepot();
        var put = Put(await depot.AlerteCapteurs.AsNoTracking().SingleAsync());
        put.Nom = "Panne Hydro";

        await new AlerteCapteursController(depot).Modifier(_idCapteur, put, CancellationToken.None);

        Assert.Null(Assert.Single(await LireEpisodes()).DateFin);
    }

    [Fact]
    public async Task Desactivation_FermeLEpisodeSansValeur()
    {
        await (await CreerServiceAvecAlerte()).EvaluerAsync(_idCapteur, 0, T0, CancellationToken.None);

        var depot = CreerDepot();
        var put = Put(await depot.AlerteCapteurs.AsNoTracking().SingleAsync());

        await new AlerteCapteursController(depot).Desactiver(_idCapteur, _idAlerte, put, CancellationToken.None);

        var episode = Assert.Single(await LireEpisodes());
        Assert.NotNull(episode.DateFin);
        Assert.Null(episode.ValeurFin);
        Assert.Equal(RaisonFinEpisode.AlerteDesactivee, episode.RaisonFin);
    }

    [Fact]
    public async Task SuppressionDeLAlerte_ConserveLHistorique()
    {
        var service = await CreerServiceAvecAlerte();
        await service.EvaluerAsync(_idCapteur, 0, T0, CancellationToken.None);
        await service.EvaluerAsync(_idCapteur, 1, T0.AddMinutes(8), CancellationToken.None);
        await service.EvaluerAsync(_idCapteur, 0, T0.AddHours(1), CancellationToken.None);

        await new AlerteCapteursController(CreerDepot()).Supprimer(_idCapteur, _idAlerte, CancellationToken.None);

        var episodes = await LireEpisodes();
        Assert.Equal(2, episodes.Length);
        Assert.All(episodes, e => Assert.Null(e.IdAlerteCapteur));
        Assert.All(episodes, e => Assert.Equal("Panne de courant", e.NomAlerte));
        Assert.Equal(RaisonFinEpisode.RetourNormale, episodes[0].RaisonFin);
        Assert.Equal(RaisonFinEpisode.AlerteSupprimee, episodes[1].RaisonFin);
        Assert.Empty(await CreerDepot().AlerteCapteurs.ToArrayAsync());
    }

    [Fact]
    public async Task Lister_GardeLesEpisodesQuiChevauchentLaPeriode()
    {
        var service = await CreerServiceAvecAlerte();
        // Hier : 23 h 50 à 0 h 10, puis aujourd'hui une panne encore en cours
        await service.EvaluerAsync(_idCapteur, 0, T0.AddDays(-1), CancellationToken.None);
        await service.EvaluerAsync(_idCapteur, 1, T0.AddDays(-1).AddMinutes(20), CancellationToken.None);
        await service.EvaluerAsync(_idCapteur, 0, T0.AddDays(-3), CancellationToken.None); // en différé, ignorée
        await service.EvaluerAsync(_idCapteur, 0, T0, CancellationToken.None);

        var controller = new EpisodesAlerteController(CreerDepot());

        var tous = (await controller.Lister(_idCapteur, null, null, CancellationToken.None)).ToArray();
        Assert.Equal(2, tous.Length);
        Assert.Equal(T0, tous[0].DateDebut); // le plus récent d'abord

        // Une période qui commence pendant le premier épisode l'inclut
        var chevauchement = await controller.Lister(_idCapteur, T0.AddDays(-1).AddMinutes(10), T0.AddDays(-1).AddHours(1), CancellationToken.None);
        Assert.Equal(T0.AddDays(-1), Assert.Single(chevauchement).DateDebut);

        // L'épisode en cours s'étend jusqu'à maintenant
        var plusTard = await controller.Lister(_idCapteur, T0.AddHours(5), null, CancellationToken.None);
        Assert.Equal(T0, Assert.Single(plusTard).DateDebut);

        Assert.Empty(await controller.Lister(_idCapteur, T0.AddDays(-10), T0.AddDays(-5), CancellationToken.None));
    }

    [Fact]
    public async Task ListerErabliere_NeRetourneQueLesCapteursDeLErabliere()
    {
        var service = await CreerServiceAvecAlerte();
        await service.EvaluerAsync(_idCapteur, 0, T0, CancellationToken.None);

        using (var depot = CreerDepot())
        {
            var autreErabliere = Guid.NewGuid();
            var autreCapteur = Guid.NewGuid();
            depot.Erabliere.Add(new Erabliere { Id = autreErabliere, Nom = "Voisin" });
            depot.Capteurs.Add(new Capteur { Id = autreCapteur, IdErabliere = autreErabliere, Nom = "Secteur" });
            depot.EpisodesAlerteCapteur.Add(new EpisodeAlerteCapteur { Id = Guid.NewGuid(), IdCapteur = autreCapteur, DateDebut = T0 });
            await depot.SaveChangesAsync();
        }

        var episodes = await new EpisodesAlerteController(CreerDepot()).ListerErabliere(_idErabliere, null, null, CancellationToken.None);

        Assert.Equal(_idCapteur, Assert.Single(episodes).IdCapteur);
    }
}
