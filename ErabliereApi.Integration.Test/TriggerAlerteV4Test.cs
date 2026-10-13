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
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace ErabliereApi.Integration.Test;

public class TriggerAlerteV4Test : IClassFixture<ErabliereApiApplicationFactory<Startup>>
{
    private readonly ErabliereApiApplicationFactory<Startup> _factory;
    private readonly IFixture _fixture;
    private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public TriggerAlerteV4Test(ErabliereApiApplicationFactory<Startup> factory)
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

        Assert.Empty(await EnvoyerPlusieursDonneesCapeur(client, id, [
            new() { IdCapteur = capteurId, V = 100 },
            new() { IdCapteur = capteurId, V = 550 }
        ]));

        // Deux données sous le seuil dans le même lot : un seul message
        Assert.StartsWith("Début d'alerte : Humidité sous-sol", Assert.Single(await EnvoyerPlusieursDonneesCapeur(client, id, [
            new() { IdCapteur = capteurId, V = 10 },
            new() { IdCapteur = capteurId, V = 20 }
        ])));
    }

    [Fact]
    public async Task WhenMaxIsSet()
    {
        var client = CreateClient();

        var id = await CreateErabiere(client);

        // When only max is set
        var capteurId = await CréerCapteurEtAlerte(client, id, null, 40m);

        Assert.StartsWith("Début d'alerte", Assert.Single(await EnvoyerPlusieursDonneesCapeur(client, id, [
            new() { IdCapteur = capteurId, V = 100 },
            new() { IdCapteur = capteurId, V = 550 }
        ])));

        Assert.StartsWith("Fin d'alerte", Assert.Single(await EnvoyerPlusieursDonneesCapeur(client, id, [
            new() { IdCapteur = capteurId, V = 10 }
        ])));
    }

    [Fact]
    public async Task WhenBothMinMaxAreSet()
    {
        var client = CreateClient();

        var id = await CreateErabiere(client);

        // When both values are set on valid values are in between
        var capteurId = await CréerCapteurEtAlerte(client, id, 40m, 70m);

        var debut = DateTimeOffset.Now;

        var sujets = await EnvoyerPlusieursDonneesCapeur(client, id, [
            new() { IdCapteur = capteurId, V = 10, D = debut },
            new() { IdCapteur = capteurId, V = 90, D = debut.AddMinutes(1) },
            new() { IdCapteur = capteurId, V = 55, D = debut.AddMinutes(2) }
        ]);

        Assert.Equal(2, sujets.Length);
        Assert.StartsWith("Début d'alerte", sujets[0]);
        Assert.StartsWith("Fin d'alerte", sujets[1]);
    }

    [Fact]
    public async Task UnLotEnDiffereEtDesordonneEstEvalueEnOrdreChronologique()
    {
        var client = CreateClient();

        var id = await CreateErabiere(client);

        var capteurId = await CréerCapteurEtAlerte(client, id, 0m, null);

        var panne = DateTimeOffset.Now.AddHours(-6);

        // L'appareil envoie après coup la panne et le retour, le retour en premier
        var sujets = await EnvoyerPlusieursDonneesCapeur(client, id, [
            new() { IdCapteur = capteurId, V = 1, D = panne.AddMinutes(30) },
            new() { IdCapteur = capteurId, V = 0, D = panne.AddMinutes(10) },
            new() { IdCapteur = capteurId, V = 0, D = panne }
        ]);

        Assert.Equal(2, sujets.Length);
        Assert.StartsWith("Début d'alerte", sujets[0]);
        Assert.StartsWith("Fin d'alerte", sujets[1]);
    }

    [Fact]
    public async Task UnLotRefuseNeDeclenchePasLAlerte()
    {
        var client = CreateClient();

        var id = await CreateErabiere(client);
        var autreErabliere = await CreateErabiere(client);

        var capteurId = await CréerCapteurEtAlerte(client, id, 0m, null);

        // Le capteur n'appartient pas à l'érablière de la route : 404, rien n'est enregistré
        var response = await client.PostAsJsonAsync(
            $"Erablieres/{autreErabliere}/Capteurs/DonneesCapteurV2/PostMany",
            new PostDonneeCapteurV2[] { new() { IdCapteur = capteurId, V = 0 } });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

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
    /// Envoie un lot de données et retourne les sujets des courriels d'alerte qu'il a causés.
    /// </summary>
    private async Task<string[]> EnvoyerPlusieursDonneesCapeur(HttpClient client, Guid idErabliere, PostDonneeCapteurV2[] postmanyDonneeCapteur)
    {
        var response = await client.PostAsJsonAsync($"Erablieres/{idErabliere}/Capteurs/DonneesCapteurV2/PostMany", postmanyDonneeCapteur);

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
