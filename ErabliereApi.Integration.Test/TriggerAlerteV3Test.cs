using AutoFixture;
using ErabliereApi.Donnees;
using ErabliereApi.Donnees.Action.Post;
using ErabliereApi.Integration.Test.ApplicationFactory;
using ErabliereApi.Services;
using ErabliereApi.Services.Notifications;
using ErabliereApi.Test.Autofixture;
using Microsoft.AspNetCore.Mvc.Testing;
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

        await AttendreEnvois();

        var emailService = _factory.Services.GetRequiredService<IEmailService>();

        var sujets = emailService.ReceivedCalls()
                                 .Select(c => ((MimeMessage)c.GetArguments()[0]!).Subject)
                                 .ToArray();

        emailService.ClearReceivedCalls();

        return sujets;
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

    private async Task<Guid> CréerCapteurEtAlerte(HttpClient client, Guid id, decimal? min, decimal? max)
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
