using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Donnees.Action.Post;
using ErabliereApi.Integration.Test.ApplicationFactory;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System;
using System.Linq;
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

    /// <summary>
    /// Régression (over-posting) : un attaquant qui possède l'érablière A ne doit pas pouvoir
    /// créer une entaille dans l'érablière B d'une victime en l'imbriquant dans la propriété
    /// de navigation "entailles" d'une ligne postée sur sa propre érablière.
    /// </summary>
    [Fact]
    public async Task PostLigne_NePeutPasCreerEntailleDansUneAutreErabliere()
    {
        var (clientAttaquant, idErabliereA) = await CreerClientEtErabliere();
        var (_, idErabliereVictimeB) = await CreerClientEtErabliere();

        var payload = new
        {
            idErabliere = idErabliereA,
            nom = "ligne-anodine",
            typeLigne = "principale",
            coordonneesJson = "[[-71.25,46.82],[-71.26,46.83]]",
            entailles = new[]
            {
                new
                {
                    idErabliere = idErabliereVictimeB,
                    nom = "PWNED",
                    latitude = 0.0,
                    longitude = 0.0
                }
            }
        };

        var response = await clientAttaquant.PostAsJsonAsync($"/Erablieres/{idErabliereA}/LignesTubelure", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ErabliereDbContext>();

        var entaillesVictime = await context.Entailles.AsNoTracking()
            .Where(e => e.IdErabliere == idErabliereVictimeB)
            .ToListAsync();

        entaillesVictime.ShouldBeEmpty("Aucune entaille ne doit être créée dans l'érablière de la victime.");
    }

    /// <summary>
    /// Régression (over-posting) : un attaquant ne doit pas pouvoir créer un arbre dans
    /// l'érablière d'une victime en l'imbriquant dans la propriété de navigation "arbre"
    /// d'une entaille postée sur sa propre érablière.
    /// </summary>
    [Fact]
    public async Task PostEntaille_NePeutPasCreerArbreDansUneAutreErabliere()
    {
        var (clientAttaquant, idErabliereA) = await CreerClientEtErabliere();
        var (_, idErabliereVictimeB) = await CreerClientEtErabliere();

        var payload = new
        {
            idErabliere = idErabliereA,
            nom = "entaille-anodine",
            latitude = 46.82,
            longitude = -71.25,
            arbre = new
            {
                idErabliere = idErabliereVictimeB,
                nom = "ARBRE-PWNED",
                espece = "Injecte",
                latitude = 0.0,
                longitude = 0.0
            }
        };

        var response = await clientAttaquant.PostAsJsonAsync($"/Erablieres/{idErabliereA}/Entailles", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ErabliereDbContext>();

        var arbresVictime = await context.Arbres.AsNoTracking()
            .Where(a => a.IdErabliere == idErabliereVictimeB)
            .ToListAsync();

        arbresVictime.ShouldBeEmpty("Aucun arbre ne doit être créé dans l'érablière de la victime.");
    }

    /// <summary>
    /// Régression (over-posting) : un PUT sur une ligne avec une érablière imbriquée dans la
    /// propriété de navigation ne doit pas modifier l'érablière de la victime.
    /// </summary>
    [Fact]
    public async Task PutLigne_NeModifiePasErabliereImbriquee()
    {
        var (clientAttaquant, idErabliereA) = await CreerClientEtErabliere();
        var (_, idErabliereVictimeB) = await CreerClientEtErabliere();

        string nomOriginalVictime;

        using (var scopeAvant = _factory.Services.CreateScope())
        {
            var ctx = scopeAvant.ServiceProvider.GetRequiredService<ErabliereDbContext>();
            var victime = await ctx.Erabliere.AsNoTracking().FirstAsync(e => e.Id == idErabliereVictimeB);
            nomOriginalVictime = victime.Nom!;
        }

        var responseCreation = await clientAttaquant.PostAsJsonAsync($"/Erablieres/{idErabliereA}/LignesTubelure", new LigneTubelure
        {
            IdErabliere = idErabliereA,
            Nom = "L-01",
            TypeLigne = "principale",
            CoordonneesJson = "[[-71.25,46.82],[-71.26,46.83]]"
        });

        responseCreation.StatusCode.ShouldBe(HttpStatusCode.OK, await responseCreation.Content.ReadAsStringAsync());
        var idLigne = await LireIdReponse(responseCreation);

        var payload = new
        {
            id = idLigne,
            idErabliere = idErabliereA,
            nom = "L-01",
            typeLigne = "principale",
            coordonneesJson = "[[-71.25,46.82],[-71.26,46.83]]",
            erabliere = new
            {
                id = idErabliereVictimeB,
                nom = "ERABLIERE-VOLEE",
                isPublic = true
            }
        };

        var response = await clientAttaquant.PutAsJsonAsync($"/Erablieres/{idErabliereA}/LignesTubelure", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ErabliereDbContext>();
        var victimeApres = await context.Erabliere.AsNoTracking().FirstAsync(e => e.Id == idErabliereVictimeB);

        victimeApres.Nom.ShouldBe(nomOriginalVictime, "L'érablière de la victime ne doit pas être modifiée.");
        victimeApres.IsPublic.ShouldBeFalse("L'érablière de la victime ne doit pas devenir publique.");
    }

    /// <summary>
    /// Régression : un PUT ne peut modifier qu'une ligne appartenant à l'érablière de la route.
    /// Tenter de modifier une ligne d'une autre érablière retourne 404.
    /// </summary>
    [Fact]
    public async Task PutLigne_DUneAutreErabliere_RetourneNotFound()
    {
        var (clientAttaquant, idErabliereA) = await CreerClientEtErabliere();
        var (_, idErabliereVictimeB) = await CreerClientEtErabliere();

        // Une ligne existante appartenant à la victime B
        Guid idLigneVictime;

        using (var scope = _factory.Services.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<ErabliereDbContext>();
            var ligneVictime = new LigneTubelure
            {
                IdErabliere = idErabliereVictimeB,
                Nom = "victime",
                TypeLigne = "principale",
                CoordonneesJson = "[[-71.25,46.82],[-71.26,46.83]]"
            };
            await ctx.LignesTubelure.AddAsync(ligneVictime);
            await ctx.SaveChangesAsync();
            idLigneVictime = ligneVictime.Id!.Value;
        }

        // L'attaquant tente de la modifier via sa propre érablière A
        var response = await clientAttaquant.PutAsJsonAsync($"/Erablieres/{idErabliereA}/LignesTubelure", new LigneTubelure
        {
            Id = idLigneVictime,
            IdErabliere = idErabliereA,
            Nom = "detourne",
            TypeLigne = "principale",
            CoordonneesJson = "[[-71.25,46.82],[-71.26,46.83]]"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound, await response.Content.ReadAsStringAsync());

        using var scopeApres = _factory.Services.CreateScope();
        var contextApres = scopeApres.ServiceProvider.GetRequiredService<ErabliereDbContext>();
        var ligneApres = await contextApres.LignesTubelure.AsNoTracking().FirstAsync(l => l.Id == idLigneVictime);

        ligneApres.Nom.ShouldBe("victime", "La ligne de la victime ne doit pas être modifiée.");
        ligneApres.IdErabliere.ShouldBe(idErabliereVictimeB, "La ligne ne doit pas être volée par l'attaquant.");
    }

    /// <summary>
    /// Cas positif : un PUT légitime met bien à jour les champs autorisés de la ligne.
    /// </summary>
    [Fact]
    public async Task PutLigne_MetAJourLesChampsLegitimes()
    {
        var (client, idErabliere) = await CreerClientEtErabliere();

        var responseCreation = await client.PostAsJsonAsync($"/Erablieres/{idErabliere}/LignesTubelure", new LigneTubelure
        {
            IdErabliere = idErabliere,
            Nom = "avant",
            TypeLigne = "principale",
            CoordonneesJson = "[[-71.25,46.82],[-71.26,46.83]]"
        });

        responseCreation.StatusCode.ShouldBe(HttpStatusCode.OK, await responseCreation.Content.ReadAsStringAsync());
        var idLigne = await LireIdReponse(responseCreation);

        var response = await client.PutAsJsonAsync($"/Erablieres/{idErabliere}/LignesTubelure", new LigneTubelure
        {
            Id = idLigne,
            IdErabliere = idErabliere,
            Nom = "apres",
            TypeLigne = "Laterale",
            CoordonneesJson = "[[-71.25,46.82],[-71.26,46.83],[-71.27,46.84]]"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ErabliereDbContext>();
        var ligne = await context.LignesTubelure.AsNoTracking().FirstAsync(l => l.Id == idLigne);

        ligne.Nom.ShouldBe("apres");
        ligne.TypeLigne.ShouldBe("laterale");
        ligne.CoordonneesJson.ShouldBe("[[-71.25,46.82],[-71.26,46.83],[-71.27,46.84]]");
    }
}
