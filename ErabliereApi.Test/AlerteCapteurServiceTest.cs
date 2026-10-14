using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Services.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ErabliereApi.Test;

/// <summary>
/// Le cas d'usage de référence : un capteur « alimentation secteur » envoie 0 pendant la panne
/// et 1 au retour, avec une alerte dont MinValue = 0.
/// </summary>
public class AlerteCapteurServiceTest
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 13, 14, 2, 0, TimeSpan.FromHours(-4));

    private readonly string _nomBD = Guid.NewGuid().ToString();
    private readonly Guid _idCapteur = Guid.NewGuid();
    private readonly Guid _idAlerte = Guid.NewGuid();
    private readonly FileNotificationsAlerte _file = new();

    private ErabliereDbContext CreerDepot()
    {
        return new ErabliereDbContext(new DbContextOptionsBuilder<ErabliereDbContext>().UseInMemoryDatabase(_nomBD).Options);
    }

    private AlerteCapteurService CreerService(ErabliereDbContext depot)
    {
        return new AlerteCapteurService(depot, _file, NullLogger<AlerteCapteurService>.Instance);
    }

    private async Task<AlerteCapteurService> CreerServiceAvecAlerte(Action<AlerteCapteur>? configurer = null)
    {
        var depot = CreerDepot();
        var idErabliere = Guid.NewGuid();

        depot.Erabliere.Add(new Erabliere { Id = idErabliere, Nom = "Érablière du Rang 4" });
        depot.Capteurs.Add(new Capteur { Id = _idCapteur, IdErabliere = idErabliere, Nom = "Secteur", Symbole = "V" });

        var alerte = new AlerteCapteur
        {
            Id = _idAlerte,
            IdCapteur = _idCapteur,
            Nom = "Panne de courant",
            MinValue = 0,
            IsEnable = true,
            EnvoyerA = "client@test.com",
            TexterA = "+15555550100"
        };
        configurer?.Invoke(alerte);
        depot.AlerteCapteurs.Add(alerte);

        await depot.SaveChangesAsync();

        return CreerService(CreerDepot());
    }

    private async Task<AlerteCapteur> LireAlerte()
    {
        return await CreerDepot().AlerteCapteurs.AsNoTracking().SingleAsync(a => a.Id == _idAlerte);
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
    public async Task Panne_EnvoieUnSeulMessageDeDebut_TantQueLaPanneDure()
    {
        var service = await CreerServiceAvecAlerte();

        await service.EvaluerAsync(_idCapteur, 0, T0, CancellationToken.None);
        await service.EvaluerAsync(_idCapteur, 0, T0.AddMinutes(5), CancellationToken.None);
        await service.EvaluerAsync(_idCapteur, 0, T0.AddMinutes(10), CancellationToken.None);

        var notification = Assert.Single(ViderFile());
        Assert.StartsWith("Début d'alerte : Panne de courant", notification.Sujet);
        Assert.StartsWith("DÉBUT D'ALERTE : Panne de courant", notification.Message);
        Assert.DoesNotContain("FIN", notification.Message);
        Assert.Equal("client@test.com", notification.EnvoyerA);
        Assert.Equal("+15555550100", notification.TexterA);

        var alerte = await LireAlerte();
        Assert.True(alerte.EstEnAlerte);
        Assert.Equal(T0, alerte.LastOccurence);
        Assert.Equal(T0, alerte.DateChangementEtat);
    }

    [Fact]
    public async Task RetourDuCourant_EnvoieUnMessageDeFinAvecLaDuree()
    {
        var service = await CreerServiceAvecAlerte();

        await service.EvaluerAsync(_idCapteur, 0, T0, CancellationToken.None);
        ViderFile();

        await service.EvaluerAsync(_idCapteur, 1, T0.AddMinutes(45), CancellationToken.None);
        await service.EvaluerAsync(_idCapteur, 1, T0.AddMinutes(50), CancellationToken.None);

        var notification = Assert.Single(ViderFile());
        Assert.StartsWith("Fin d'alerte : Panne de courant", notification.Sujet);
        Assert.StartsWith("FIN D'ALERTE : Panne de courant", notification.Message);
        Assert.DoesNotContain("DÉBUT D'ALERTE", notification.Message);
        Assert.Contains("Durée : 45 min", notification.Message);
        Assert.Contains($"Début de l'alerte : {AlerteCapteurMessage.FormaterDate(T0)}", notification.Message);

        var alerte = await LireAlerte();
        Assert.False(alerte.EstEnAlerte);
        Assert.Equal(T0, alerte.LastOccurence);
        Assert.Equal(T0.AddMinutes(45), alerte.DateChangementEtat);
    }

    [Fact]
    public async Task Message_NommeLAlerteLeCapteurLErabliereEtLHeureDeLaDonnee()
    {
        var service = await CreerServiceAvecAlerte();

        await service.EvaluerAsync(_idCapteur, 0, T0, CancellationToken.None);

        var notification = Assert.Single(ViderFile());
        Assert.Equal("Début d'alerte : Panne de courant - Secteur (Érablière du Rang 4)", notification.Sujet);
        Assert.Contains("Capteur : Secteur", notification.Message);
        Assert.Contains("Érablière : Érablière du Rang 4", notification.Message);
        Assert.Contains("Valeur : 0 V", notification.Message);
        // La date de la donnée, pas l'heure de réception
        Assert.Contains($"Heure de la donnée : {AlerteCapteurMessage.FormaterDate(T0)}", notification.Message);
    }

    [Fact]
    public async Task DonneeNormale_NEnvoieRien()
    {
        var service = await CreerServiceAvecAlerte();

        await service.EvaluerAsync(_idCapteur, 1, T0, CancellationToken.None);

        Assert.Empty(ViderFile());
        var alerte = await LireAlerte();
        Assert.False(alerte.EstEnAlerte);
        Assert.Null(alerte.LastOccurence);
    }

    [Fact]
    public async Task PannesSuccessives_EnvoientUnDebutEtUneFinChacune()
    {
        var service = await CreerServiceAvecAlerte();

        await service.EvaluerAsync(_idCapteur, 0, T0, CancellationToken.None);
        await service.EvaluerAsync(_idCapteur, 1, T0.AddMinutes(10), CancellationToken.None);
        await service.EvaluerAsync(_idCapteur, 0, T0.AddHours(3), CancellationToken.None);
        await service.EvaluerAsync(_idCapteur, 1, T0.AddHours(5).AddMinutes(5), CancellationToken.None);

        var notifications = ViderFile();
        Assert.Equal(4, notifications.Count);
        Assert.StartsWith("DÉBUT", notifications[0].Message);
        Assert.StartsWith("FIN", notifications[1].Message);
        Assert.Contains("Durée : 10 min", notifications[1].Message);
        Assert.StartsWith("DÉBUT", notifications[2].Message);
        Assert.StartsWith("FIN", notifications[3].Message);
        Assert.Contains("Durée : 2 h 5 min", notifications[3].Message);
    }

    [Fact]
    public async Task DonneeEnDiffere_PlusAncienneQueLeRetour_NeRelancePasLAlerte()
    {
        var service = await CreerServiceAvecAlerte();

        await service.EvaluerAsync(_idCapteur, 0, T0, CancellationToken.None);
        await service.EvaluerAsync(_idCapteur, 1, T0.AddMinutes(30), CancellationToken.None);
        ViderFile();

        // Donnée gardée en file par l'appareil pendant la panne de réseau, reçue après le retour
        await service.EvaluerAsync(_idCapteur, 0, T0.AddMinutes(15), CancellationToken.None);

        Assert.Empty(ViderFile());
        var alerte = await LireAlerte();
        Assert.False(alerte.EstEnAlerte);
        Assert.Equal(T0.AddMinutes(30), alerte.DateChangementEtat);
    }

    [Fact]
    public async Task DonneeEnDiffere_PlusAncienneQueLaPanne_NeTerminePasLAlerte()
    {
        var service = await CreerServiceAvecAlerte();

        await service.EvaluerAsync(_idCapteur, 0, T0, CancellationToken.None);
        ViderFile();

        await service.EvaluerAsync(_idCapteur, 1, T0.AddMinutes(-20), CancellationToken.None);

        Assert.Empty(ViderFile());
        Assert.True((await LireAlerte()).EstEnAlerte);
    }

    [Fact]
    public async Task DonneesEnDiffere_RecuesEnOrdre_AnnoncentLaPanneEtLeRetourALeurHeure()
    {
        var service = await CreerServiceAvecAlerte();

        // L'appareil était hors ligne : la panne et le retour arrivent ensemble, bien après coup
        await service.EvaluerAsync(_idCapteur, 0, T0.AddDays(-1), CancellationToken.None);
        await service.EvaluerAsync(_idCapteur, 1, T0.AddDays(-1).AddHours(2), CancellationToken.None);

        var notifications = ViderFile();
        Assert.Equal(2, notifications.Count);
        Assert.Contains($"Heure de la donnée : {AlerteCapteurMessage.FormaterDate(T0.AddDays(-1))}", notifications[0].Message);
        Assert.Contains("Durée : 2 h", notifications[1].Message);
    }

    [Fact]
    public async Task MemeDonneeRenvoyee_NEnvoieQuUnMessage()
    {
        var service = await CreerServiceAvecAlerte();

        // Un client qui croit à un échec renvoie la même donnée
        for (var i = 0; i < 8; i++)
        {
            await service.EvaluerAsync(_idCapteur, 0, T0, CancellationToken.None);
        }

        Assert.Single(ViderFile());
    }

    [Fact]
    public async Task ChangementDEtatSimultane_NEnvoieQuUnMessage()
    {
        await CreerServiceAvecAlerte();

        // Deux requêtes lisent l'alerte à l'état normal avant que l'une ou l'autre l'enregistre
        var depot1 = CreerDepot();
        var depot2 = CreerDepot();
        await depot1.AlerteCapteurs.LoadAsync();
        await depot2.AlerteCapteurs.LoadAsync();

        await CreerService(depot1).EvaluerAsync(_idCapteur, 0, T0, CancellationToken.None);
        await CreerService(depot2).EvaluerAsync(_idCapteur, 0, T0.AddSeconds(1), CancellationToken.None);

        Assert.Single(ViderFile());
        var alerte = await LireAlerte();
        Assert.True(alerte.EstEnAlerte);
        Assert.Equal(T0, alerte.LastOccurence);
    }

    [Fact]
    public async Task Marge_EviteLaRafaleQuandLaValeurOscilleAutourDuSeuil()
    {
        var service = await CreerServiceAvecAlerte(a =>
        {
            a.MinValue = 10;
            a.Marge = 2;
        });

        decimal[] valeurs = [9.9m, 10.1m, 9.8m, 10.4m, 11.9m, 9.9m, 12.5m, 11m, 9.9m];

        for (var i = 0; i < valeurs.Length; i++)
        {
            await service.EvaluerAsync(_idCapteur, valeurs[i], T0.AddMinutes(i), CancellationToken.None);
        }

        var notifications = ViderFile();
        Assert.Equal(3, notifications.Count);
        Assert.StartsWith("DÉBUT", notifications[0].Message);
        Assert.Contains("Valeur : 9.9 V", notifications[0].Message);
        Assert.StartsWith("FIN", notifications[1].Message);
        Assert.Contains("Valeur : 12.5 V", notifications[1].Message);
        Assert.StartsWith("DÉBUT", notifications[2].Message);
    }

    [Fact]
    public async Task AlerteDesactivee_NEnvoieRienEtGardeSonEtat()
    {
        var service = await CreerServiceAvecAlerte(a => a.IsEnable = false);

        await service.EvaluerAsync(_idCapteur, 0, T0, CancellationToken.None);

        Assert.Empty(ViderFile());
        Assert.False((await LireAlerte()).EstEnAlerte);
    }

    [Fact]
    public async Task DonneeDUnAutreCapteur_NeToucheNiLAlerteNiLaFile()
    {
        var service = await CreerServiceAvecAlerte();

        await service.EvaluerAsync(Guid.NewGuid(), 0, T0, CancellationToken.None);

        Assert.Empty(ViderFile());
        Assert.False((await LireAlerte()).EstEnAlerte);
    }

    [Theory]
    [InlineData(0, "moins d'une minute")]
    [InlineData(45, "45 min")]
    [InlineData(120, "2 h")]
    [InlineData(125, "2 h 5 min")]
    [InlineData(1635, "1 j 3 h 15 min")]
    public void Duree_EstLisible(int minutes, string attendu)
    {
        Assert.Equal(attendu, AlerteCapteurMessage.FormaterDuree(TimeSpan.FromMinutes(minutes)));
    }
}
