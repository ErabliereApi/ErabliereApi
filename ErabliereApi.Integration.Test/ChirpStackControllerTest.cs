using AutoFixture;
using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Donnees.Action.Post;
using ErabliereApi.Integration.Test.ApplicationFactory;
using ErabliereApi.Test.Autofixture;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace ErabliereApi.Integration.Test;

public class ChirpStackControllerTest : IClassFixture<ErabliereApiApplicationFactory<Startup>>
{
    private readonly ErabliereApiApplicationFactory<Startup> _factory;
    private readonly IFixture _fixture;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ChirpStackControllerTest(ErabliereApiApplicationFactory<Startup> factory)
    {
        _factory = factory;
        _fixture = ErabliereFixture.CreerFixture();
    }

    [Fact]
    public async Task SimpleCall_RandomInformtion_BadRequest()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true,
            HandleCookies = true,
            MaxAutomaticRedirections = 7
        });

        var content = new StringContent(Constants.ChirpStackExBadRequest, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/chirpstack/events?event=up", content);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SimpleCall_ServerExist_BadRequestOnErabliereId()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true,
            HandleCookies = true,
            MaxAutomaticRedirections = 7
        });

        var payload = JsonSerializer.Deserialize<PostChirpstackEvent>(Constants.ChirpStackExNoErabliereId, _serializerOptions);
        Assert.NotNull(payload);
        var configC = new StringContent(JsonSerializer.Serialize(payload.deviceInfo), Encoding.UTF8, "application/json");
        var responseC = await client.PostAsync("/chirpstack/configs", configC);
        responseC.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);

        var content = new StringContent(Constants.ChirpStackExNoErabliereId, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/chirpstack/events?event=up", content);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SimpleCall_ServerErabliereAndCapteurExist_Ok()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true,
            HandleCookies = true,
            MaxAutomaticRedirections = 7
        });

        var cr = await client.PostAsJsonAsync("/erablieres", _fixture.Create<PostErabliere>());
        Assert.Equal(System.Net.HttpStatusCode.OK, cr.StatusCode);
        var erablieres = await client.GetFromJsonAsync<Erabliere[]>("/erablieres?$top=1&?$select=id");
        Assert.NotNull(erablieres);
        var erabliere = Assert.Single(erablieres);
        var idErabliere = Assert.NotNull(erabliere.Id);
        var payloadStr = Constants.ChirpStackExOk.Replace("<replace-guid-erabliere>", idErabliere.ToString());
        Assert.Contains(erabliere.Id.Value.ToString(), payloadStr);
        var payload = JsonSerializer.Deserialize<PostChirpstackEvent>(payloadStr, _serializerOptions);
        Assert.NotNull(payload);
        // Une vraie mesure du paquet (7 à 10 sont la batterie, l'intervalle et le matériel), pour qu'une donnée soit enregistrée.
        using var decode = JsonDocument.Parse(await client.GetStringAsync($"/chirpstack/decode?data={System.Uri.EscapeDataString(payload.data)}"));
        var idMesure = decode.RootElement.GetProperty("mesurements").EnumerateArray()
            .Select(m => m.GetProperty("mesure").GetInt32())
            .First(m => m is < 7 or > 10);
        var capt1 = await client.PostAsJsonAsync($"/erablieres/{idErabliere}/capteurs", new PostCapteur
        {
            Nom = "Temperature du sol arrière",
            Symbole = "C",
            ExternalId = payload.deviceInfo.devEui,
            IdErabliere = idErabliere,
            IdMesure = idMesure,
        });
        var capt2 = await client.PostAsJsonAsync($"/erablieres/{idErabliere}/capteurs", new PostCapteur
        {
            Nom = "Humidité du sol arrière",
            Symbole = "%",
            ExternalId = payload.deviceInfo.devEui,
            IdErabliere = idErabliere,
        });
        var configC = new StringContent(JsonSerializer.Serialize(payload.deviceInfo), Encoding.UTF8, "application/json");
        var responseC = await client.PostAsync("/chirpstack/configs", configC);
        responseC.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);

        var content = new StringContent(payloadStr, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/chirpstack/events?event=up", content);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);

        // Le silence des capteurs se mesure sur la réception : chaque capteur qui a reçu une donnée l'a notée.
        using var scope = _factory.Services.CreateScope();
        var depot = scope.ServiceProvider.GetRequiredService<ErabliereDbContext>();
        var capteursAvecDonnees = await depot.Capteurs.AsNoTracking()
            .Where(c => c.IdErabliere == idErabliere && c.DonneesCapteur.Any())
            .ToArrayAsync();
        Assert.NotEmpty(capteursAvecDonnees);
        Assert.All(capteursAvecDonnees, c => Assert.NotNull(c.DateDerniereReception));
    }
}