using ErabliereApi.Controllers;
using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Donnees.Action.Put;
using ErabliereApi.Services.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ErabliereApi.Test;

/// <summary>
/// Le cas d'usage : un kit de détection de panne de courant ne peut plus rien envoyer quand le
/// routeur du client tombe avec le courant. Le silence est alors le seul signal.
/// </summary>
public class SilenceCapteurServiceTest
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 14, 14, 32, 0, TimeSpan.FromHours(-4));

    private readonly string _nomBD = Guid.NewGuid().ToString();
    private readonly Guid _idCapteur = Guid.NewGuid();
    private readonly Guid _idAlerte = Guid.NewGuid();
    private readonly FileNotificationsAlerte _file = new();
    private readonly Horloge _horloge = new() { Maintenant = T0 };

    private ErabliereDbContext CreerDepot(params IInterceptor[] intercepteurs)
    {
        return new ErabliereDbContext(new DbContextOptionsBuilder<ErabliereDbContext>()
            .UseInMemoryDatabase(_nomBD)
            .AddInterceptors(intercepteurs)
            .Options);
    }

    private SilenceCapteurService CreerService(ErabliereDbContext? depot = null)
    {
        return new SilenceCapteurService(depot ?? CreerDepot(), _file, _horloge, NullLogger<SilenceCapteurService>.Instance);
    }

    private async Task CreerAlerte(Action<AlerteCapteur>? configurer = null, DateTimeOffset? derniereReception = null)
    {
        var depot = CreerDepot();
        var idErabliere = Guid.NewGuid();

        depot.Erabliere.Add(new Erabliere { Id = idErabliere, Nom = "Érablière du Rang 4" });
        depot.Capteurs.Add(new Capteur
        {
            Id = _idCapteur,
            IdErabliere = idErabliere,
            Nom = "Kit panne de courant",
            DateDerniereReception = derniereReception ?? T0
        });

        var alerte = new AlerteCapteur
        {
            Id = _idAlerte,
            IdCapteur = _idCapteur,
            Nom = "Silence du kit",
            IsEnable = true,
            DelaiSilenceMinutes = 3,
            EnvoyerA = "client@test.com",
            TexterA = "+15555550100"
        };
        configurer?.Invoke(alerte);
        depot.AlerteCapteurs.Add(alerte);

        await depot.SaveChangesAsync();
    }

    private async Task<AlerteCapteur> LireAlerte()
    {
        return await CreerDepot().AlerteCapteurs.AsNoTracking().SingleAsync(a => a.Id == _idAlerte);
    }

    private async Task<EpisodeAlerteCapteur[]> LireEpisodes()
    {
        return await CreerDepot().EpisodesAlerteCapteur.AsNoTracking().OrderBy(e => e.DateDebut).ToArrayAsync();
    }

    private List<NotificationAlerte> ViderFile()
    {
        var notifications = new List<NotificationAlerte>();

        while (_file.TryLire(out var notification))
        {
            notifications.Add(notification);
        }

        return notifications;
    }

    [Fact]
    public async Task SansDelaiConfigure_AucunMessage()
    {
        await CreerAlerte(a => a.DelaiSilenceMinutes = null);
        _horloge.Maintenant = T0.AddDays(2);

        Assert.Equal(0, await CreerService().DetecterSilencesAsync(CancellationToken.None));

        Assert.Empty(ViderFile());
        Assert.Null((await LireAlerte()).EnSilenceDepuis);
    }

    [Fact]
    public async Task AvantLeDelai_AucunMessage()
    {
        await CreerAlerte();
        _horloge.Maintenant = T0.AddMinutes(2).AddSeconds(59);

        await CreerService().DetecterSilencesAsync(CancellationToken.None);

        Assert.Empty(ViderFile());
    }

    [Fact]
    public async Task MuetPlusQueLeDelai_UnMessageHonneteSurLaCause()
    {
        await CreerAlerte();
        _horloge.Maintenant = T0.AddMinutes(3);

        Assert.Equal(1, await CreerService().DetecterSilencesAsync(CancellationToken.None));

        var message = Assert.Single(ViderFile());
        Assert.Equal(_idAlerte, message.IdAlerte);
        Assert.Equal("Capteur muet : Kit panne de courant (Érablière du Rang 4)", message.Sujet);
        Assert.Contains($"Plus de nouvelles depuis {AlerteCapteurMessage.FormaterDate(T0)} : panne de courant ou perte de réseau.", message.Message);
        Assert.Contains("Le serveur ne peut pas distinguer les deux", message.Message);
        Assert.Contains("Délai de silence : 3 min", message.Message);
        Assert.Equal("client@test.com", message.EnvoyerA);
        Assert.Equal("+15555550100", message.TexterA);

        Assert.Equal(T0, (await LireAlerte()).EnSilenceDepuis);
    }

    [Fact]
    public async Task SilenceQuiDure_UnSeulMessageParEpisode()
    {
        await CreerAlerte();

        for (var minutes = 3; minutes <= 60; minutes++)
        {
            _horloge.Maintenant = T0.AddMinutes(minutes);
            await CreerService().DetecterSilencesAsync(CancellationToken.None);
        }

        Assert.Single(ViderFile());
        var episode = Assert.Single(await LireEpisodes());
        Assert.Equal(TypeEpisodeAlerte.Silence, episode.Type);
        Assert.Equal(T0, episode.DateDebut);
        Assert.Null(episode.ValeurDebut);
        Assert.Null(episode.DateFin);
    }

    [Fact]
    public async Task Reprise_UnSecondMessageAvecLaDureeDuSilence()
    {
        await CreerAlerte();
        _horloge.Maintenant = T0.AddMinutes(3);
        await CreerService().DetecterSilencesAsync(CancellationToken.None);
        ViderFile();

        _horloge.Maintenant = T0.AddMinutes(38);
        await CreerService().SignalerReceptionAsync([_idCapteur], CancellationToken.None);

        var message = Assert.Single(ViderFile());
        Assert.Equal("Données reprises : Kit panne de courant (Érablière du Rang 4)", message.Sujet);
        Assert.Contains("Durée du silence : 38 min", message.Message);

        Assert.Null((await LireAlerte()).EnSilenceDepuis);
        var episode = Assert.Single(await LireEpisodes());
        Assert.Equal(T0.AddMinutes(38), episode.DateFin);
        Assert.Equal(RaisonFinEpisode.DonneesReprises, episode.RaisonFin);
    }

    [Fact]
    public async Task Reception_RepousseLeSilence()
    {
        await CreerAlerte();

        _horloge.Maintenant = T0.AddMinutes(2);
        await CreerService().SignalerReceptionAsync([_idCapteur], CancellationToken.None);

        _horloge.Maintenant = T0.AddMinutes(4);
        await CreerService().DetecterSilencesAsync(CancellationToken.None);

        Assert.Empty(ViderFile());

        _horloge.Maintenant = T0.AddMinutes(5);
        await CreerService().DetecterSilencesAsync(CancellationToken.None);

        Assert.Single(ViderFile());
    }

    [Fact]
    public async Task ReceptionSansSilence_AucunMessage()
    {
        await CreerAlerte();
        _horloge.Maintenant = T0.AddMinutes(1);

        await CreerService().SignalerReceptionAsync([_idCapteur], CancellationToken.None);

        Assert.Empty(ViderFile());
        Assert.Equal(T0.AddMinutes(1), (await CreerDepot().Capteurs.AsNoTracking().SingleAsync()).DateDerniereReception);
    }

    [Fact]
    public async Task AucuneReceptionConnue_CompteDepuisLaModificationDeLAlerte()
    {
        await CreerAlerte(a => a.DM = T0);

        var depot = CreerDepot();
        (await depot.Capteurs.SingleAsync()).DateDerniereReception = null;
        await depot.SaveChangesAsync();

        _horloge.Maintenant = T0.AddMinutes(3);
        await CreerService().DetecterSilencesAsync(CancellationToken.None);

        var message = Assert.Single(ViderFile());
        Assert.Contains($"Aucune donnée reçue depuis le début de la surveillance, {AlerteCapteurMessage.FormaterDate(T0)}", message.Message);
    }

    [Fact]
    public async Task AlerteDesactivee_PasSurveillee()
    {
        await CreerAlerte(a => a.IsEnable = false);
        _horloge.Maintenant = T0.AddHours(1);

        await CreerService().DetecterSilencesAsync(CancellationToken.None);

        Assert.Empty(ViderFile());
    }

    [Fact]
    public async Task DeuxInstances_UneSeuleEnvoieLeMessage()
    {
        await CreerAlerte();
        _horloge.Maintenant = T0.AddMinutes(3);

        // L'instance A a lu l'alerte ; l'instance B commence le silence avant que A n'enregistre.
        var autreInstance = new AvantEnregistrement(() => CreerService().DetecterSilencesAsync(CancellationToken.None));

        var commencesParA = await CreerService(CreerDepot(autreInstance)).DetecterSilencesAsync(CancellationToken.None);

        Assert.True(autreInstance.Fait);
        Assert.Equal(0, commencesParA);
        Assert.Single(ViderFile());
        Assert.Single(await LireEpisodes());
    }

    [Fact]
    public async Task DeuxReceptionsSimultanees_UnSeulMessageDeReprise()
    {
        await CreerAlerte();
        _horloge.Maintenant = T0.AddMinutes(3);
        await CreerService().DetecterSilencesAsync(CancellationToken.None);
        ViderFile();

        _horloge.Maintenant = T0.AddMinutes(10);

        // A a enregistré la réception et lu l'alerte en silence ; B termine le silence avant A.
        var autreRequete = new AvantEnregistrement(
            () => CreerService().SignalerReceptionAsync([_idCapteur], CancellationToken.None),
            quandAlerteModifiee: true);

        await CreerService(CreerDepot(autreRequete)).SignalerReceptionAsync([_idCapteur], CancellationToken.None);

        Assert.True(autreRequete.Fait);
        Assert.Single(ViderFile());
    }

    [Fact]
    public async Task SilencePendantUneDonnee_LeChangementDeSeuilNEstPasPerdu()
    {
        await CreerAlerte(a => a.MinValue = 0);
        _horloge.Maintenant = T0.AddMinutes(3);

        // La surveillance modifie la même ligne pendant qu'une donnée sous le seuil est évaluée.
        var surveillance = new AvantEnregistrement(
            () => CreerService().DetecterSilencesAsync(CancellationToken.None),
            quandAlerteModifiee: true);

        var alerteService = new AlerteCapteurService(CreerDepot(surveillance), _file, NullLogger<AlerteCapteurService>.Instance);
        await alerteService.EvaluerAsync(_idCapteur, 0, T0.AddMinutes(3), CancellationToken.None);

        Assert.True(surveillance.Fait);
        var alerte = await LireAlerte();
        Assert.True(alerte.EstEnAlerte);
        Assert.NotNull(alerte.EnSilenceDepuis);
        Assert.Equal(2, ViderFile().Count);
    }

    [Fact]
    public async Task FinDAlerteDeSeuil_NeFermePasLEpisodeDeSilence()
    {
        await CreerAlerte(a => a.MinValue = 0);
        var alerteService = new AlerteCapteurService(CreerDepot(), _file, NullLogger<AlerteCapteurService>.Instance);
        await alerteService.EvaluerAsync(_idCapteur, 0, T0, CancellationToken.None);

        _horloge.Maintenant = T0.AddMinutes(3);
        await CreerService().DetecterSilencesAsync(CancellationToken.None);

        alerteService = new AlerteCapteurService(CreerDepot(), _file, NullLogger<AlerteCapteurService>.Instance);
        await alerteService.EvaluerAsync(_idCapteur, 1, T0.AddMinutes(1), CancellationToken.None);

        var episodes = await LireEpisodes();
        Assert.Equal(RaisonFinEpisode.RetourNormale, episodes.Single(e => e.Type == TypeEpisodeAlerte.Seuil).RaisonFin);
        Assert.Null(episodes.Single(e => e.Type == TypeEpisodeAlerte.Silence).DateFin);
    }

    [Fact]
    public async Task DesactiverPendantLeSilence_FermeLEpisodeSansMessage()
    {
        await CreerAlerte();
        _horloge.Maintenant = T0.AddMinutes(3);
        await CreerService().DetecterSilencesAsync(CancellationToken.None);
        ViderFile();

        var depot = CreerDepot();
        var alerte = await depot.AlerteCapteurs.AsNoTracking().SingleAsync();
        await new AlerteCapteursController(depot).Desactiver(_idCapteur, _idAlerte, Put(alerte), CancellationToken.None);

        Assert.Null((await LireAlerte()).EnSilenceDepuis);
        Assert.Equal(RaisonFinEpisode.AlerteDesactivee, Assert.Single(await LireEpisodes()).RaisonFin);

        await CreerService().SignalerReceptionAsync([_idCapteur], CancellationToken.None);

        Assert.Empty(ViderFile());
    }

    [Fact]
    public async Task ModifierLeDelaiPendantLeSilence_SignaleDeNouveauSelonLeNouveauDelai()
    {
        await CreerAlerte();
        _horloge.Maintenant = T0.AddMinutes(3);
        await CreerService().DetecterSilencesAsync(CancellationToken.None);
        ViderFile();

        var depot = CreerDepot();
        var put = Put(await depot.AlerteCapteurs.AsNoTracking().SingleAsync());
        put.DelaiSilenceMinutes = 30;
        await new AlerteCapteursController(depot).Modifier(_idCapteur, put, CancellationToken.None);

        Assert.Equal(RaisonFinEpisode.AlerteModifiee, Assert.Single(await LireEpisodes()).RaisonFin);

        _horloge.Maintenant = T0.AddMinutes(29);
        await CreerService().DetecterSilencesAsync(CancellationToken.None);
        Assert.Empty(ViderFile());

        _horloge.Maintenant = T0.AddMinutes(30);
        await CreerService().DetecterSilencesAsync(CancellationToken.None);
        Assert.Single(ViderFile());
    }

    [Fact]
    public async Task ModifierLesSeuils_NeTouchePasAuSilence()
    {
        await CreerAlerte();
        _horloge.Maintenant = T0.AddMinutes(3);
        await CreerService().DetecterSilencesAsync(CancellationToken.None);

        var depot = CreerDepot();
        var put = Put(await depot.AlerteCapteurs.AsNoTracking().SingleAsync());
        put.MaxValue = 100;
        await new AlerteCapteursController(depot).Modifier(_idCapteur, put, CancellationToken.None);

        Assert.Equal(T0, (await LireAlerte()).EnSilenceDepuis);
        Assert.Null(Assert.Single(await LireEpisodes()).DateFin);
    }

    private static PutAlerteCapteur Put(AlerteCapteur alerte)
    {
        return new PutAlerteCapteur
        {
            Id = alerte.Id,
            IdCapteur = alerte.IdCapteur,
            Nom = alerte.Nom,
            EnvoyerA = alerte.EnvoyerA,
            TexterA = alerte.TexterA,
            MinValue = alerte.MinValue,
            MaxValue = alerte.MaxValue,
            Marge = alerte.Marge,
            DelaiSilenceMinutes = alerte.DelaiSilenceMinutes,
            IsEnable = alerte.IsEnable
        };
    }

    private sealed class Horloge : TimeProvider
    {
        public DateTimeOffset Maintenant { get; set; }

        public override DateTimeOffset GetUtcNow() => Maintenant.ToUniversalTime();
    }

    /// <summary>
    /// Fait passer une autre instance juste avant le premier enregistrement d'une alerte modifiée
    /// par ce contexte : rejoue la course entre deux instances de façon déterministe.
    /// </summary>
    private sealed class AvantEnregistrement : SaveChangesInterceptor
    {
        private readonly Func<Task> _autre;
        private readonly bool _quandAlerteModifiee;
        private bool _fait;

        public bool Fait => _fait;

        public AvantEnregistrement(Func<Task> autre, bool quandAlerteModifiee = false)
        {
            _autre = autre;
            _quandAlerteModifiee = quandAlerteModifiee;
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            var alerteModifiee = eventData.Context!.ChangeTracker.Entries<AlerteCapteur>().Any(e => e.State == EntityState.Modified);

            if (!_fait && (alerteModifiee || !_quandAlerteModifiee))
            {
                _fait = true;

                await _autre();
            }

            return result;
        }
    }
}
