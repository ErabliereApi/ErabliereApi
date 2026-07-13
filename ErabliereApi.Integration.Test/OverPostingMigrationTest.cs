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

/// <summary>
/// Régression (anti over-posting) pour les contrôleurs migrés vers des DTO : Baril (nav imbriquée
/// + PUT cross-tenant) et Donnees/Importer (override de la clé étrangère d'érablière).
/// </summary>
public class OverPostingMigrationTest : IClassFixture<StripeEnabledApplicationFactory<Startup>>
{
    private readonly StripeEnabledApplicationFactory<Startup> _factory;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public OverPostingMigrationTest(StripeEnabledApplicationFactory<Startup> factory)
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
            Nom = $"OPM-{Guid.NewGuid()}",
            IpRules = "-"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var erabliere = JsonSerializer.Deserialize<Erabliere>(
            await response.Content.ReadAsStringAsync(), _jsonOptions);

        Assert.NotNull(erabliere?.Id);

        return (client, erabliere.Id.Value);
    }

    [Fact]
    public async Task PostBaril_AvecErabliereImbriquee_NeModifiePasErabliereVictime()
    {
        var (clientAttaquant, idErabliereA) = await CreerClientEtErabliere();
        var (_, idErabliereVictimeB) = await CreerClientEtErabliere();

        string nomOriginalVictime;

        using (var scopeAvant = _factory.Services.CreateScope())
        {
            var ctx = scopeAvant.ServiceProvider.GetRequiredService<ErabliereDbContext>();
            nomOriginalVictime = (await ctx.Erabliere.AsNoTracking().FirstAsync(e => e.Id == idErabliereVictimeB)).Nom!;
        }

        var payload = new
        {
            idErabliere = idErabliereA,
            q = "AA",
            erabliere = new
            {
                id = idErabliereVictimeB,
                nom = "ERABLIERE-VOLEE",
                isPublic = true
            }
        };

        var response = await clientAttaquant.PostAsJsonAsync($"/Erablieres/{idErabliereA}/Baril", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ErabliereDbContext>();

        var victime = await context.Erabliere.AsNoTracking().FirstAsync(e => e.Id == idErabliereVictimeB);
        victime.Nom.ShouldBe(nomOriginalVictime, "L'érablière de la victime ne doit pas être modifiée.");
        victime.IsPublic.ShouldBeFalse();
    }

    [Fact]
    public async Task PutBaril_DUneAutreErabliere_RetourneNotFound()
    {
        var (clientAttaquant, idErabliereA) = await CreerClientEtErabliere();
        var (_, idErabliereVictimeB) = await CreerClientEtErabliere();

        Guid idBarilVictime;

        using (var scope = _factory.Services.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<ErabliereDbContext>();
            var baril = new Baril { IdErabliere = idErabliereVictimeB, Q = "original" };
            await ctx.Barils.AddAsync(baril);
            await ctx.SaveChangesAsync();
            idBarilVictime = baril.Id!.Value;
        }

        var response = await clientAttaquant.PutAsJsonAsync($"/Erablieres/{idErabliereA}/Baril", new
        {
            id = idBarilVictime,
            idErabliere = idErabliereA,
            q = "detourne"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound, await response.Content.ReadAsStringAsync());

        using var scopeApres = _factory.Services.CreateScope();
        var contextApres = scopeApres.ServiceProvider.GetRequiredService<ErabliereDbContext>();
        var barilApres = await contextApres.Barils.AsNoTracking().FirstAsync(b => b.Id == idBarilVictime);

        barilApres.Q.ShouldBe("original", "Le baril de la victime ne doit pas être modifié.");
        barilApres.IdErabliere.ShouldBe(idErabliereVictimeB, "Le baril ne doit pas être volé.");
    }

    [Fact]
    public async Task ImporterDonnees_IgnoreLIdErabliereDuCorps_EtUtiliseCelleDeLaRoute()
    {
        var (clientAttaquant, idErabliereA) = await CreerClientEtErabliere();
        var (_, idErabliereVictimeB) = await CreerClientEtErabliere();

        var payload = new[]
        {
            new { idErabliere = idErabliereVictimeB, t = (short)10, v = (short)20, nb = (short)30 }
        };

        var response = await clientAttaquant.PostAsJsonAsync($"/Erablieres/{idErabliereA}/Donnees/Importer", payload);

        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ErabliereDbContext>();

        var donneesVictime = await context.Donnees.AsNoTracking()
            .Where(d => d.IdErabliere == idErabliereVictimeB)
            .ToListAsync();
        donneesVictime.ShouldBeEmpty("Aucune donnée ne doit être importée dans l'érablière de la victime.");

        var donneesA = await context.Donnees.AsNoTracking()
            .Where(d => d.IdErabliere == idErabliereA)
            .ToListAsync();
        donneesA.Count.ShouldBe(1, "La donnée doit être importée dans l'érablière de la route.");
    }
}
