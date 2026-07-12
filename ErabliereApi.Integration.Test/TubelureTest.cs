using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Donnees.Action.Post;
using ErabliereApi.Integration.Test.ApplicationFactory;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace ErabliereApi.Integration.Test;

public class TubelureTest : IClassFixture<StripeEnabledApplicationFactory<Startup>>
{
    private readonly StripeEnabledApplicationFactory<Startup> _factory;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TubelureTest(StripeEnabledApplicationFactory<Startup> factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient client, Guid idErabliere)> CreerClientEtErabliere()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true,
            HandleCookies = true,
            MaxAutomaticRedirections = 7
        });

        var (_, apiKey) = await _factory.CreateValidApiKeyAsync();

        client.DefaultRequestHeaders.Add("X-ErabliereApi-ApiKey", apiKey);

        var response = await client.PostAsJsonAsync("/Erablieres", new PostErabliere
        {
            Nom = $"ET-{Guid.NewGuid()}",
            IpRules = "-"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var erabliere = JsonSerializer.Deserialize<Erabliere>(
            await response.Content.ReadAsStringAsync(), _jsonOptions);

        Assert.NotNull(erabliere?.Id);

        return (client, erabliere.Id.Value);
    }

    private static async Task<Guid> LireIdReponse(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        return document.RootElement.GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task PosterLigneValide()
    {
        var (client, idErabliere) = await CreerClientEtErabliere();

        var response = await client.PostAsJsonAsync($"/Erablieres/{idErabliere}/LignesTubelure", new LigneTubelure
        {
            IdErabliere = idErabliere,
            Nom = "M-01",
            TypeLigne = "Principale",
            CoordonneesJson = "[[-71.25,46.82],[-71.26,46.83],[-71.27,46.84]]"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var idLigne = await LireIdReponse(response);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ErabliereDbContext>();
        var ligne = await context.LignesTubelure.FirstOrDefaultAsync(l => l.Id == idLigne);

        Assert.NotNull(ligne);
        Assert.Equal(idErabliere, ligne.IdErabliere);
        Assert.Equal("M-01", ligne.Nom);
        Assert.Equal("principale", ligne.TypeLigne);
        Assert.Equal("[[-71.25,46.82],[-71.26,46.83],[-71.27,46.84]]", ligne.CoordonneesJson);
        Assert.NotNull(ligne.DC);
    }

    [Fact]
    public async Task PosterLigneTypeInvalide()
    {
        var (client, idErabliere) = await CreerClientEtErabliere();

        var response = await client.PostAsJsonAsync($"/Erablieres/{idErabliere}/LignesTubelure", new LigneTubelure
        {
            IdErabliere = idErabliere,
            TypeLigne = "diagonale",
            CoordonneesJson = "[[-71.25,46.82],[-71.26,46.83]]"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task PosterLigneCoordonneesInvalides()
    {
        var (client, idErabliere) = await CreerClientEtErabliere();

        // Un seul point
        var response = await client.PostAsJsonAsync($"/Erablieres/{idErabliere}/LignesTubelure", new LigneTubelure
        {
            IdErabliere = idErabliere,
            TypeLigne = "laterale",
            CoordonneesJson = "[[-71.25,46.82]]"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, await response.Content.ReadAsStringAsync());

        // Latitude hors borne
        response = await client.PostAsJsonAsync($"/Erablieres/{idErabliere}/LignesTubelure", new LigneTubelure
        {
            IdErabliere = idErabliere,
            TypeLigne = "laterale",
            CoordonneesJson = "[[-71.25,95],[-71.26,46.83]]"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, await response.Content.ReadAsStringAsync());

        // JSON invalide
        response = await client.PostAsJsonAsync($"/Erablieres/{idErabliere}/LignesTubelure", new LigneTubelure
        {
            IdErabliere = idErabliere,
            TypeLigne = "laterale",
            CoordonneesJson = "pas du json"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ArbreEntailleGeoJsonEtSuppression()
    {
        var (client, idErabliere) = await CreerClientEtErabliere();

        // Ajouter une ligne
        var responseLigne = await client.PostAsJsonAsync($"/Erablieres/{idErabliere}/LignesTubelure", new LigneTubelure
        {
            IdErabliere = idErabliere,
            Nom = "L-01",
            TypeLigne = "laterale",
            CoordonneesJson = "[[-71.25,46.82],[-71.26,46.83]]"
        });

        responseLigne.StatusCode.ShouldBe(HttpStatusCode.OK, await responseLigne.Content.ReadAsStringAsync());
        var idLigne = await LireIdReponse(responseLigne);

        // Ajouter un arbre
        var responseArbre = await client.PostAsJsonAsync($"/Erablieres/{idErabliere}/Arbres", new Arbre
        {
            IdErabliere = idErabliere,
            Nom = "A-01",
            Espece = "Érable à sucre",
            Latitude = 46.825,
            Longitude = -71.255
        });

        responseArbre.StatusCode.ShouldBe(HttpStatusCode.OK, await responseArbre.Content.ReadAsStringAsync());
        var idArbre = await LireIdReponse(responseArbre);

        // Ajouter une entaille liée à l'arbre et à la ligne
        var responseEntaille = await client.PostAsJsonAsync($"/Erablieres/{idErabliere}/Entailles", new Entaille
        {
            IdErabliere = idErabliere,
            Nom = "E-01",
            Latitude = 46.8251,
            Longitude = -71.2551,
            IdArbre = idArbre,
            IdLigneTubelure = idLigne
        });

        responseEntaille.StatusCode.ShouldBe(HttpStatusCode.OK, await responseEntaille.Content.ReadAsStringAsync());
        var idEntaille = await LireIdReponse(responseEntaille);

        // GeoJson : une FeatureCollection avec 3 features
        var responseGeoJson = await client.GetAsync($"/Erablieres/{idErabliere}/Tubelure/GeoJson");

        responseGeoJson.StatusCode.ShouldBe(HttpStatusCode.OK, await responseGeoJson.Content.ReadAsStringAsync());

        using var geoJson = JsonDocument.Parse(await responseGeoJson.Content.ReadAsStringAsync());

        Assert.Equal("FeatureCollection", geoJson.RootElement.GetProperty("type").GetString());
        Assert.Equal(3, geoJson.RootElement.GetProperty("features").GetArrayLength());

        // Supprimer l'arbre : l'entaille survit, mais son lien vers l'arbre est retiré
        var responseDelete = await client.DeleteAsync($"/Erablieres/{idErabliere}/Arbres/{idArbre}");

        responseDelete.StatusCode.ShouldBe(HttpStatusCode.NoContent, await responseDelete.Content.ReadAsStringAsync());

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ErabliereDbContext>();
        var entaille = await context.Entailles.AsNoTracking().FirstOrDefaultAsync(e => e.Id == idEntaille);

        Assert.NotNull(entaille);
        Assert.Null(entaille.IdArbre);
        Assert.Equal(idLigne, entaille.IdLigneTubelure);
    }

    [Fact]
    public async Task PosterEntailleAvecArbreDUneAutreErabliere()
    {
        var (client, idErabliereA) = await CreerClientEtErabliere();
        var (_, idErabliereB) = await CreerClientEtErabliere();

        // L'arbre appartient à l'érablière B
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ErabliereDbContext>();
        var arbre = new Arbre
        {
            IdErabliere = idErabliereB,
            Latitude = 46.82,
            Longitude = -71.25
        };
        await context.Arbres.AddAsync(arbre);
        await context.SaveChangesAsync();

        // L'entaille de l'érablière A ne peut pas référencer l'arbre de l'érablière B
        var response = await client.PostAsJsonAsync($"/Erablieres/{idErabliereA}/Entailles", new Entaille
        {
            IdErabliere = idErabliereA,
            Latitude = 46.82,
            Longitude = -71.25,
            IdArbre = arbre.Id
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, await response.Content.ReadAsStringAsync());
    }
}
