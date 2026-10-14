using AutoFixture;
using ErabliereApi.Donnees;
using ErabliereApi.Donnees.Action.Post;
using ErabliereApi.Integration.Test.ApplicationFactory;
using ErabliereApi.Depot.Sql;
using ErabliereApi.Services;
using ErabliereApi.Services.Notifications;
using ErabliereApi.Test.Autofixture;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MimeKit;
using NSubstitute;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ErabliereApi.Integration.Test;

public class TriggerAlerteV3Test : IClassFixture<ErabliereApiApplicationFactory<Startup>>
{
    private readonly ErabliereApiApplicationFactory<Startup> _factory;
    private readonly IFixture _fixture;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public TriggerAlerteV3Test(ErabliereApiApplicationFactory<Startup> factory)
    {
        _factory = factory;
        _fixture = ErabliereFixture.CreerFixture();
    }

    [Fact]
    public async Task WhenMinIsSet()
    {
        var client = CreateClient();

        var id = await CreateErabiere(client);

        // When only min is set
        var capteurId = await CréerCapteurEtAlerte(client, id, 40m, null);

        Assert.StartsWith("Début d'alerte : Humidité sous-sol", Assert.Single(await EnvoyerDonneesCapeur(client, capteurId, 10m)));
        Assert.Empty(await EnvoyerDonneesCapeur(client, capteurId, 5m));
        Assert.StartsWith("Fin d'alerte : Humidité sous-sol", Assert.Single(await EnvoyerDonneesCapeur(client, capteurId, 55m)));
        Assert.Empty(await EnvoyerDonneesCapeur(client, capteurId, 60m));
    }

    [Fact]
    public async Task WhenMaxIsSet()
    {
        var client = CreateClient();

        var id = await CreateErabiere(client);

        // When only max is set
        var capteurId = await CréerCapteurEtAlerte(client, id, null, 40m);

        Assert.Empty(await EnvoyerDonneesCapeur(client, capteurId, 10m));
        Assert.StartsWith("Début d'alerte", Assert.Single(await EnvoyerDonneesCapeur(client, capteurId, 55m)));
        Assert.StartsWith("Fin d'alerte", Assert.Single(await EnvoyerDonneesCapeur(client, capteurId, 10m)));
    }

    [Fact]
    public async Task WhenBothMinMaxAreSet()
    {
        var client = CreateClient();

        var id = await CreateErabiere(client);

        // When both values are set on valid values are in between
        var capteurId = await CréerCapteurEtAlerte(client, id, 40m, 70m);

        Assert.StartsWith("Début d'alerte", Assert.Single(await EnvoyerDonneesCapeur(client, capteurId, 10m)));
        // Toujours hors seuil : l'alerte reste en cours, sans nouveau message
        Assert.Empty(await EnvoyerDonneesCapeur(client, capteurId, 90m));
        Assert.StartsWith("Fin d'alerte", Assert.Single(await EnvoyerDonneesCapeur(client, capteurId, 55m)));
    }

    [Fact]
    public async Task LaReponseNAttendPasLEnvoiDuCourriel()
    {
        var client = CreateClient();

        var id = await CreateErabiere(client);

        var capteurId = await CréerCapteurEtAlerte(client, id, 0m, null);

        var emailService = _factory.Services.GetRequiredService<IEmailService>();
        emailService.SendEmailAsync(Arg.Any<MimeMessage>(), Arg.Any<CancellationToken>())
                    .Returns(_ => Task.Delay(TimeSpan.FromSeconds(3)));

        try
        {
            var chrono = Stopwatch.StartNew();

            var response = await client.PostAsJsonAsync($"Capteurs/{capteurId}/DonneesCapteurV2", new PostDonneeCapteurV2
            {
                IdCapteur = capteurId,
                V = 0m
            });

            chrono.Stop();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(chrono.Elapsed < TimeSpan.FromSeconds(2), $"La réponse a pris {chrono.ElapsedMilliseconds} ms");

            await AttendreEnvois();

            Assert.Single(emailService.ReceivedCalls());
        }
        finally
        {
            emailService.SendEmailAsync(Arg.Any<MimeMessage>(), Arg.Any<CancellationToken>())
                        .Returns(Task.CompletedTask);
            emailService.ClearReceivedCalls();
        }
    }

    [Fact]
    public async Task UneDonneeRefuseeNeDeclenchePasLAlerte()
    {
        var client = CreateClient();

        var id = await CreateErabiere(client);

        var capteurId = await CréerCapteurEtAlerte(client, id, 0m, null);

        // L'id du corps ne concorde pas avec celui de la route : 400, la donnée n'est pas enregistrée
        var response = await client.PostAsJsonAsync($"Capteurs/{capteurId}/DonneesCapteurV2", new PostDonneeCapteurV2
        {
            IdCapteur = Guid.NewGuid(),
            V = 0m
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await AttendreEnvois();

        Assert.Empty(_factory.Services.GetRequiredService<IEmailService>().ReceivedCalls());
    }

    [Fact]
    public async Task LaPanneEstHistoriseeCommeUnEpisode()
    {
        var client = CreateClient();

        var id = await CreateErabiere(client);

        var capteurId = await CréerCapteurEtAlerte(client, id, 0m, null);
        var debut = new DateTimeOffset(2026, 10, 13, 13, 46, 0, TimeSpan.FromHours(-4));

        await EnvoyerDonneeDatee(client, capteurId, 0m, debut);

        var enCours = Assert.Single(await LireEpisodes(client, $"Capteurs/{capteurId}/EpisodesAlerte"));
        Assert.Equal(debut, enCours.DateDebut);
        Assert.Null(enCours.DateFin);

        await EnvoyerDonneeDatee(client, capteurId, 1m, debut.AddMinutes(8));

        var termine = Assert.Single(await LireEpisodes(client, $"Erablieres/{id}/EpisodesAlerte"));
        Assert.Equal(capteurId, termine.IdCapteur);
        Assert.Equal(debut.AddMinutes(8), termine.DateFin);
        Assert.Equal(RaisonFinEpisode.RetourNormale, termine.RaisonFin);

        Assert.Empty(await LireEpisodes(client, $"Capteurs/{capteurId}/EpisodesAlerte?debut={Uri.EscapeDataString(debut.AddDays(1).ToString("O"))}"));
    }

    [Fact]
    public async Task SupprimerAvecUnCorps_NeSupprimeQueLesAlertesDuCapteurDeLaRoute()
    {
        var client = CreateClient();

        var id = await CreateErabiere(client);

        var monCapteur = await CréerCapteurEtAlerte(client, id, 0m, null);
        var autreCapteur = await CréerCapteurEtAlerte(client, id, 0m, null);

        var alertes = await client.GetFromJsonAsync<AlerteCapteur[]>($"Capteurs/{autreCapteur}/AlerteCapteurs", _serializerOptions);
        var alerteAutre = Assert.Single(alertes!);

        // Le corps complet, comme l'envoie le proxy, mais avec l'alerte d'un autre capteur
        alerteAutre.IdCapteur = monCapteur;
        var refuse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"Capteurs/{monCapteur}/AlerteCapteurs")
        {
            Content = JsonContent.Create(alerteAutre, options: _serializerOptions)
        });

        Assert.Equal(HttpStatusCode.NotFound, refuse.StatusCode);
        Assert.Single((await client.GetFromJsonAsync<AlerteCapteur[]>($"Capteurs/{autreCapteur}/AlerteCapteurs", _serializerOptions))!);

        var mienne = Assert.Single((await client.GetFromJsonAsync<AlerteCapteur[]>($"Capteurs/{monCapteur}/AlerteCapteurs", _serializerOptions))!);
        var accepte = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"Capteurs/{monCapteur}/AlerteCapteurs")
        {
            Content = JsonContent.Create(mienne, options: _serializerOptions)
        });

        Assert.Equal(HttpStatusCode.NoContent, accepte.StatusCode);
        Assert.Empty((await client.GetFromJsonAsync<AlerteCapteur[]>($"Capteurs/{monCapteur}/AlerteCapteurs", _serializerOptions))!);
    }

    [Fact]
    public async Task UneDonneeDateeDuPasse_TermineLeSilence()
    {
        var client = CreateClient();

        var id = await CreateErabiere(client);

        var capteurId = await CréerCapteurEtAlerte(client, id, null, null, delaiSilenceMinutes: 3);

        Assert.StartsWith("Capteur muet : ", Assert.Single(await RendreMuet(capteurId)));

        // L'appareil renvoie en différé une donnée de l'avant-veille : c'est la réception qui compte.
        var response = await client.PostAsJsonAsync($"Capteurs/{capteurId}/DonneesCapteurV2", new PostDonneeCapteurV2
        {
            IdCapteur = capteurId,
            V = 1m,
            D = DateTimeOffset.Now.AddDays(-2)
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("Données reprises : ", Assert.Single(await LireSujetsEnvoyes()));
        Assert.Contains(await LireEpisodes(client, $"Capteurs/{capteurId}/EpisodesAlerte"),
            e => e.Type == TypeEpisodeAlerte.Silence && e.RaisonFin == RaisonFinEpisode.DonneesReprises);
    }

    [Fact]
    public async Task UnLotTermineLeSilence()
    {
        var client = CreateClient();

        var id = await CreateErabiere(client);

        var capteurId = await CréerCapteurEtAlerte(client, id, null, null, delaiSilenceMinutes: 3);

        Assert.Single(await RendreMuet(capteurId));

        var response = await client.PostAsJsonAsync($"Erablieres/{id}/Capteurs/DonneesCapteurV2/PostMany", new[]
        {
            new PostDonneeCapteurV2 { IdCapteur = capteurId, V = 1m, D = DateTimeOffset.Now.AddHours(-3) },
            new PostDonneeCapteurV2 { IdCapteur = capteurId, V = 1m, D = DateTimeOffset.Now.AddHours(-2) }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("Données reprises : ", Assert.Single(await LireSujetsEnvoyes()));
    }

    /// <summary>
    /// Recule la dernière réception du capteur d'une heure et lance une vérification des capteurs
    /// muets, sans attendre le service d'arrière-plan. Retourne les sujets des courriels envoyés.
    /// </summary>
    private async Task<string[]> RendreMuet(Guid capteurId)
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var depot = scope.ServiceProvider.GetRequiredService<ErabliereDbContext>();
            var capteur = await depot.Capteurs.SingleAsync(c => c.Id == capteurId);
            capteur.DateDerniereReception = DateTimeOffset.Now.AddHours(-1);
            await depot.SaveChangesAsync();

            await scope.ServiceProvider.GetRequiredService<SilenceCapteurService>().DetecterSilencesAsync(CancellationToken.None);
        }

        return await LireSujetsEnvoyes();
    }

    private async Task<string[]> LireSujetsEnvoyes()
    {
        await AttendreEnvois();

        var emailService = _factory.Services.GetRequiredService<IEmailService>();

        var sujets = emailService.ReceivedCalls()
                                 .Select(c => ((MimeMessage)c.GetArguments()[0]!).Subject)
                                 .ToArray();

        emailService.ClearReceivedCalls();

        return sujets;
    }

    private async Task EnvoyerDonneeDatee(HttpClient client, Guid capteurId, decimal valeur, DateTimeOffset date)
    {
        var response = await client.PostAsJsonAsync($"Capteurs/{capteurId}/DonneesCapteurV2", new PostDonneeCapteurV2
        {
            IdCapteur = capteurId,
            V = valeur,
            D = date
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await AttendreEnvois();

        _factory.Services.GetRequiredService<IEmailService>().ClearReceivedCalls();
    }

    private async Task<EpisodeAlerteCapteur[]> LireEpisodes(HttpClient client, string url)
    {
        var episodes = await client.GetFromJsonAsync<EpisodeAlerteCapteur[]>(url, _serializerOptions);

        Assert.NotNull(episodes);

        return episodes;
    }

    private HttpClient CreateClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true,
            HandleCookies = true,
            MaxAutomaticRedirections = 7
        });
    }

    /// <summary>
    /// Envoie une donnée et retourne les sujets des courriels d'alerte qu'elle a causés.
    /// </summary>
    private async Task<string[]> EnvoyerDonneesCapeur(HttpClient client, Guid capteurId, decimal valeur)
    {
        var response = await client.PostAsJsonAsync($"Capteurs/{capteurId}/DonneesCapteurV2", new PostDonneeCapteurV2
        {
            IdCapteur = capteurId,
            V = valeur
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return await LireSujetsEnvoyes();
    }

    /// <summary>
    /// L'envoi se fait en arrière-plan : attend que la file des messages soit vide.
    /// </summary>
    private async Task AttendreEnvois()
    {
        var file = _factory.Services.GetRequiredService<FileNotificationsAlerte>();

        for (var i = 0; i < 400 && file.EnAttente > 0; i++)
        {
            await Task.Delay(25);
        }

        Assert.Equal(0, file.EnAttente);
    }

    private async Task<Guid> CréerCapteurEtAlerte(HttpClient client, Guid id, decimal? min, decimal? max, int? delaiSilenceMinutes = null)
    {
        var postCapteur = _fixture.Create<PostCapteur>();
        postCapteur.IdErabliere = id;

        var capteurResponse = await client.PostAsJsonAsync($"Erablieres/{id}/Capteurs", postCapteur, _serializerOptions);

        Assert.NotNull(capteurResponse);

        var capteur = await capteurResponse.Content.ReadFromJsonAsync<Capteur>(_serializerOptions);

        Assert.NotNull(capteur);

        var alerteCapteur = new AlerteCapteur
        {
            IdCapteur = capteur.Id,
            EnvoyerA = "test@test.com",
            MaxValue = max,
            MinValue = min,
            DelaiSilenceMinutes = delaiSilenceMinutes,
            IsEnable = true,
            Nom = "Humidité sous-sol"
        };

        var alerteResponse = await client.PostAsJsonAsync($"Capteurs/{capteur.Id}/AlerteCapteurs", alerteCapteur, _serializerOptions);

        Assert.NotNull(alerteResponse);

        var alerte = await alerteResponse.Content.ReadFromJsonAsync<AlerteCapteur>(_serializerOptions);

        Assert.NotNull(alerte);

        Assert.NotNull(capteur.Id);

        return capteur.Id.Value;
    }

    private async Task<Guid> CreateErabiere(HttpClient client)
    {
        var initialResponse = await client.GetFromJsonAsync<Erabliere[]>("/Erablieres");

        Assert.NotNull(initialResponse);

        var newErabliere = _fixture.Create<PostErabliere>();

        var postResponse = await client.PostAsJsonAsync("/Erablieres", newErabliere);

        Assert.NotNull(postResponse);

        var newErabliereResponse = await postResponse.Content.ReadFromJsonAsync<Erabliere>(_serializerOptions);

        Assert.NotNull(newErabliereResponse);
        Assert.NotNull(newErabliere.Id);

        return newErabliere.Id.Value;
    }
}
