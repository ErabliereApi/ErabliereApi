using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Donnees.Action.Get;
using ErabliereApi.Integration.Test.ApplicationFactory;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace ErabliereApi.Integration.Test;

/// <summary>
/// GET /api/Abonnements/Courant est la source de vérité du forfait pour les
/// composants externes, à commencer par le serveur MCP, qui n'a pas accès à la
/// base de données et s'authentifie par clé d'api.
/// </summary>
/// <remarks>
/// Le chemin par clé d'api est justement celui qui casse facilement :
/// <c>ApiKeyAuthorizationContext</c> est enregistré en Scoped et rempli par
/// <c>ApiKeyMiddleware</c> dans la portée de la requête, donc un appelant qui le
/// lirait depuis une portée enfant recevrait une instance vide et ne
/// reconnaîtrait jamais l'utilisateur.
/// </remarks>
public class AbonnementCourantTest : IClassFixture<StripeEnabledApplicationFactory<Startup>>
{
    private readonly StripeEnabledApplicationFactory<Startup> _factory;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public AbonnementCourantTest(StripeEnabledApplicationFactory<Startup> factory)
    {
        _factory = factory;
    }

    private async Task<GetAbonnementCourant> ObtenirForfaitAsync(string key)
    {
        using var client = _factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/Abonnements/Courant");

        request.Headers.Add("X-ErabliereApi-ApiKey", key);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var forfait = JsonSerializer.Deserialize<GetAbonnementCourant>(
            await response.Content.ReadAsStringAsync(), JsonOptions);

        Assert.NotNull(forfait);

        return forfait;
    }

    private async Task AjouterAbonnementAsync(Guid customerId, string plan, StatutAbonnement statut, string? frequence = null)
    {
        using var scope = _factory.Services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ErabliereDbContext>();

        context.Abonnements.Add(new Abonnement
        {
            CustomerId = customerId,
            Plan = plan,
            Statut = statut,
            FrequenceFacturation = frequence,
            DateDebut = DateTimeOffset.Now.AddDays(-1),
            DC = DateTimeOffset.Now,
            DM = DateTimeOffset.Now
        });

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task UneCleDApiSansAbonnement_RetourneLeForfaitGratuit()
    {
        var (_, key) = await _factory.CreateValidApiKeyAsync();

        var forfait = await ObtenirForfaitAsync(key);

        Assert.Equal(ForfaitsAbonnement.Gratuit, forfait.Plan);
        Assert.False(forfait.AbonnementActif);
        Assert.Null(forfait.FrequenceFacturation);
    }

    [Fact]
    public async Task UneCleDApiAvecAbonnementActif_RetourneSonForfait()
    {
        var (customer, key) = await _factory.CreateValidApiKeyAsync();

        await AjouterAbonnementAsync(customer.Id!.Value, ForfaitsAbonnement.Base, StatutAbonnement.Actif, FrequencesFacturation.Mensuelle);

        var forfait = await ObtenirForfaitAsync(key);

        Assert.Equal(ForfaitsAbonnement.Base, forfait.Plan);
        Assert.True(forfait.AbonnementActif);
        Assert.Equal(FrequencesFacturation.Mensuelle, forfait.FrequenceFacturation);
        Assert.NotNull(forfait.DateDebut);
    }

    [Fact]
    public async Task UneCleDApiAvecAbonnementEnAttente_RetourneLeForfaitGratuit()
    {
        // Le paiement n'est pas confirmé : l'abonnement ne donne encore aucun droit.
        var (customer, key) = await _factory.CreateValidApiKeyAsync();

        await AjouterAbonnementAsync(customer.Id!.Value, ForfaitsAbonnement.Base, StatutAbonnement.EnAttente, FrequencesFacturation.Mensuelle);

        var forfait = await ObtenirForfaitAsync(key);

        Assert.Equal(ForfaitsAbonnement.Gratuit, forfait.Plan);
        Assert.False(forfait.AbonnementActif);
    }

    [Fact]
    public async Task DeuxClesDApi_NeVoientPasLeForfaitDeLAutre()
    {
        // Une régression ici donnerait le forfait d'un abonné à n'importe qui.
        var (abonne, cleAbonne) = await _factory.CreateValidApiKeyAsync();
        var (_, cleAutre) = await _factory.CreateValidApiKeyAsync();

        await AjouterAbonnementAsync(abonne.Id!.Value, ForfaitsAbonnement.Base, StatutAbonnement.Actif);

        Assert.Equal(ForfaitsAbonnement.Base, (await ObtenirForfaitAsync(cleAbonne)).Plan);
        Assert.Equal(ForfaitsAbonnement.Gratuit, (await ObtenirForfaitAsync(cleAutre)).Plan);
    }

    [Fact]
    public async Task SansCleDApiNiJeton_LAccesEstRefuse()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/api/Abonnements/Courant");

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UneCleDApiInvalide_NObtientAucunForfait()
    {
        using var client = _factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/Abonnements/Courant");

        request.Headers.Add("X-ErabliereApi-ApiKey", Guid.NewGuid().ToString());

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
