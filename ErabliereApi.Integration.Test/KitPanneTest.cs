using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Donnees.Action.Get;
using ErabliereApi.Donnees.Action.Post;
using ErabliereApi.Integration.Test.ApplicationFactory;
using ErabliereApi.Services;
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

public class KitPanneTest : IClassFixture<StripeEnabledApplicationFactory<Startup>>
{
    private const string EnteteCle = "X-ErabliereApi-ApiKey";

    private readonly StripeEnabledApplicationFactory<Startup> _factory;

    public KitPanneTest(StripeEnabledApplicationFactory<Startup> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AjouterKitPanne_CreeCapteursAlertesEtCleRestreinte()
    {
        var (customer, cleClient) = await _factory.CreateValidApiKeyAsync();
        var client = _factory.CreateClient();
        var idErabliere = await CreerErabliereAsync(client, cleClient);

        var configuration = await AjouterKitAsync(client, cleClient, idErabliere, new PostKitPanne { Nom = "Cabane", DelaiSilenceMinutes = 10 });

        using var scope = _factory.Services.CreateScope();
        var depot = scope.ServiceProvider.GetRequiredService<ErabliereDbContext>();

        var kit = await depot.Kits.SingleAsync(k => k.Id == configuration.IdKit);
        kit.IdErabliere.ShouldBe(idErabliere);
        kit.Type.ShouldBe(Kit.TypePanne);

        var capteurs = await depot.Capteurs.Where(c => c.IdErabliere == idErabliere).ToArrayAsync();
        capteurs.Select(c => c.Nom).ShouldBe(["Cabane alimentation", "Cabane charge", "Cabane autonomie"], ignoreOrder: true);

        var panne = await depot.AlerteCapteurs.SingleAsync(a => a.IdCapteur == kit.IdCapteurAlimentation);
        panne.MinValue.ShouldBe(0);
        panne.IsEnable.ShouldBeTrue();
        panne.EnvoyerA.ShouldBe(customer.Email);

        var silence = await depot.AlerteCapteurs.SingleAsync(a => a.IdCapteur == kit.IdCapteurCharge);
        silence.DelaiSilenceMinutes.ShouldBe(10);
        silence.IsEnable.ShouldBeTrue();

        var cle = await depot.ApiKeys.SingleAsync(a => a.Id == kit.IdApiKey);
        cle.CustomerId.ShouldBe(customer.Id!.Value);
        cle.SubscriptionId.ShouldBeNull();
        cle.AuthorizeVerbs.ShouldBe("POST");

        // La configuration contient les ids des capteurs du kit
        using var json = JsonDocument.Parse(configuration.FichierConfiguration);
        json.RootElement.GetProperty("authType").GetString().ShouldBe("ApiKey");
        json.RootElement.GetProperty("capteurAlimentation").GetString().ShouldBe(kit.IdCapteurAlimentation.ToString());
        json.RootElement.GetProperty("capteurCharge").GetString().ShouldBe(kit.IdCapteurCharge.ToString());
        json.RootElement.GetProperty("capteurAutonomie").GetString().ShouldBe(kit.IdCapteurAutonomie.ToString());
        configuration.NomFichierAuthentification.ShouldStartWith("auth.");
    }

    [Fact]
    public async Task AjouterKitPanne_UrlFournie_NommeLeFichierDeCleDApresSonHote()
    {
        var (_, cleClient) = await _factory.CreateValidApiKeyAsync();
        var client = _factory.CreateClient();
        var idErabliere = await CreerErabliereAsync(client, cleClient);

        var configuration = await AjouterKitAsync(client, cleClient, idErabliere, new PostKitPanne { Url = "https://erabliere.exemple.com:8443/" });

        using var json = JsonDocument.Parse(configuration.FichierConfiguration);
        json.RootElement.GetProperty("url").GetString().ShouldBe("https://erabliere.exemple.com:8443");
        configuration.NomFichierAuthentification.ShouldBe("auth.erabliere.exemple.com:8443.config");
    }

    [Fact]
    public async Task AjouterKitPanne_UrlInvalide_RetourneBadRequestSansRienCreer()
    {
        var (_, cleClient) = await _factory.CreateValidApiKeyAsync();
        var client = _factory.CreateClient();
        var idErabliere = await CreerErabliereAsync(client, cleClient);

        var reponse = await EnvoyerAsync(client, cleClient, $"/Erablieres/{idErabliere}/Kits/Panne", new PostKitPanne { Url = "ftp://erabliere.exemple.com" });

        reponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using var scope = _factory.Services.CreateScope();
        var depot = scope.ServiceProvider.GetRequiredService<ErabliereDbContext>();
        (await depot.Capteurs.AnyAsync(c => c.IdErabliere == idErabliere)).ShouldBeFalse();
        (await depot.Kits.AnyAsync(k => k.IdErabliere == idErabliere)).ShouldBeFalse();
    }

    [Fact]
    public async Task CleDuKit_EnvoieSeulementAuxCapteursDuKit()
    {
        var (_, cleClient) = await _factory.CreateValidApiKeyAsync();
        var client = _factory.CreateClient();
        var idErabliere = await CreerErabliereAsync(client, cleClient);
        var configuration = await AjouterKitAsync(client, cleClient, idErabliere, new PostKitPanne());
        var cleKit = CleDuKit(configuration);
        var idCharge = IdCapteur(configuration, "capteurCharge");

        var envoi = await EnvoyerAsync(client, cleKit, $"/Capteurs/{idCharge}/DonneesCapteurV2", new PostDonneeCapteurV2 { IdCapteur = idCharge, V = 87 });
        envoi.StatusCode.ShouldBe(HttpStatusCode.OK, await envoi.Content.ReadAsStringAsync());

        // Un autre capteur de la même érablière
        var autreCapteur = await CreerCapteurAsync(client, cleClient, idErabliere);
        var envoiAutre = await EnvoyerAsync(client, cleKit, $"/Capteurs/{autreCapteur}/DonneesCapteurV2", new PostDonneeCapteurV2 { IdCapteur = autreCapteur, V = 1 });
        envoiAutre.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        // Lecture refusée : la clé n'autorise que POST
        using var lecture = new HttpRequestMessage(HttpMethod.Get, $"/Capteurs/{idCharge}/DonneesCapteurV2");
        lecture.Headers.Add(EnteteCle, cleKit);
        (await client.SendAsync(lecture)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        // Même sur une action [AllowAnonymous], la clé n'agit pas au nom du client hors de ses routes
        using var erablieres = new HttpRequestMessage(HttpMethod.Get, "/Erablieres");
        erablieres.Headers.Add(EnteteCle, cleKit);
        (await client.SendAsync(erablieres)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        // Une clé de kit ne peut pas mettre en service un autre kit
        var kitParKit = await EnvoyerAsync(client, cleKit, $"/Erablieres/{idErabliere}/Kits/Panne", new PostKitPanne());
        kitParKit.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Revoquer_LaCleDuKitCesseDeFonctionner_EtLesCapteursRestent()
    {
        var (_, cleClient) = await _factory.CreateValidApiKeyAsync();
        var client = _factory.CreateClient();
        var idErabliere = await CreerErabliereAsync(client, cleClient);
        var configuration = await AjouterKitAsync(client, cleClient, idErabliere, new PostKitPanne());
        var idCharge = IdCapteur(configuration, "capteurCharge");

        using var suppression = new HttpRequestMessage(HttpMethod.Delete, $"/Erablieres/{idErabliere}/Kits/{configuration.IdKit}");
        suppression.Headers.Add(EnteteCle, cleClient);
        (await client.SendAsync(suppression)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var envoi = await EnvoyerAsync(client, CleDuKit(configuration), $"/Capteurs/{idCharge}/DonneesCapteurV2", new PostDonneeCapteurV2 { IdCapteur = idCharge, V = 87 });
        envoi.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        using var liste = new HttpRequestMessage(HttpMethod.Get, $"/Erablieres/{idErabliere}/Kits");
        liste.Headers.Add(EnteteCle, cleClient);
        var kits = await (await client.SendAsync(liste)).Content.ReadFromJsonAsync<GetKit[]>();
        kits.ShouldNotBeNull();
        kits.Single().DateRevocation.ShouldNotBeNull();

        using var scope = _factory.Services.CreateScope();
        var depot = scope.ServiceProvider.GetRequiredService<ErabliereDbContext>();
        (await depot.Capteurs.CountAsync(c => c.IdErabliere == idErabliere)).ShouldBe(3);
    }

    [Fact]
    public async Task Revoquer_KitDUneAutreErabliere_RetourneNotFound()
    {
        var (_, cleClient) = await _factory.CreateValidApiKeyAsync();
        var client = _factory.CreateClient();
        var monErabliere = await CreerErabliereAsync(client, cleClient);
        var autreErabliere = await CreerErabliereAsync(client, cleClient);
        var configuration = await AjouterKitAsync(client, cleClient, autreErabliere, new PostKitPanne());

        using var suppression = new HttpRequestMessage(HttpMethod.Delete, $"/Erablieres/{monErabliere}/Kits/{configuration.IdKit}");
        suppression.Headers.Add(EnteteCle, cleClient);
        (await client.SendAsync(suppression)).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var envoi = await EnvoyerAsync(client, CleDuKit(configuration), $"/Capteurs/{IdCapteur(configuration, "capteurCharge")}/DonneesCapteurV2",
            new PostDonneeCapteurV2 { IdCapteur = IdCapteur(configuration, "capteurCharge"), V = 87 });
        envoi.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SupprimerErabliere_RevoqueLaCleDuKit()
    {
        var (_, cleClient) = await _factory.CreateValidApiKeyAsync();
        var client = _factory.CreateClient();
        var idErabliere = await CreerErabliereAsync(client, cleClient);
        var configuration = await AjouterKitAsync(client, cleClient, idErabliere, new PostKitPanne());

        Guid? idCle;
        using (var scope = _factory.Services.CreateScope())
        {
            idCle = (await scope.ServiceProvider.GetRequiredService<ErabliereDbContext>().Kits.SingleAsync(k => k.Id == configuration.IdKit)).IdApiKey;
        }

        using var suppression = new HttpRequestMessage(HttpMethod.Delete, $"/Erablieres/{idErabliere}")
        {
            Content = JsonContent.Create(new { id = idErabliere })
        };
        suppression.Headers.Add(EnteteCle, cleClient);
        var reponse = await client.SendAsync(suppression);
        reponse.StatusCode.ShouldBe(HttpStatusCode.NoContent, await reponse.Content.ReadAsStringAsync());

        using (var scope = _factory.Services.CreateScope())
        {
            var depot = scope.ServiceProvider.GetRequiredService<ErabliereDbContext>();
            (await depot.Kits.AnyAsync(k => k.Id == configuration.IdKit)).ShouldBeFalse();
            (await depot.ApiKeys.SingleAsync(a => a.Id == idCle)).RevocationTime.ShouldNotBeNull();
        }
    }

    private static async Task<Guid> CreerErabliereAsync(HttpClient client, string cle)
    {
        var reponse = await EnvoyerAsync(client, cle, "/Erablieres", new PostErabliere { Nom = $"Kit-{Guid.NewGuid()}" });
        reponse.StatusCode.ShouldBe(HttpStatusCode.OK, await reponse.Content.ReadAsStringAsync());
        var erabliere = await reponse.Content.ReadFromJsonAsync<Erabliere>();
        return erabliere!.Id!.Value;
    }

    private static async Task<Guid> CreerCapteurAsync(HttpClient client, string cle, Guid idErabliere)
    {
        var reponse = await EnvoyerAsync(client, cle, $"/Erablieres/{idErabliere}/Capteurs", new PostCapteur { IdErabliere = idErabliere, Nom = "Autre" });
        reponse.StatusCode.ShouldBe(HttpStatusCode.OK, await reponse.Content.ReadAsStringAsync());
        using var json = JsonDocument.Parse(await reponse.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<PostKitPanneResponse> AjouterKitAsync(HttpClient client, string cle, Guid idErabliere, PostKitPanne kit)
    {
        var reponse = await EnvoyerAsync(client, cle, $"/Erablieres/{idErabliere}/Kits/Panne", kit);
        reponse.StatusCode.ShouldBe(HttpStatusCode.OK, await reponse.Content.ReadAsStringAsync());
        return (await reponse.Content.ReadFromJsonAsync<PostKitPanneResponse>())!;
    }

    private static async Task<HttpResponseMessage> EnvoyerAsync<T>(HttpClient client, string cle, string chemin, T corps)
    {
        using var requete = new HttpRequestMessage(HttpMethod.Post, chemin) { Content = JsonContent.Create(corps) };
        requete.Headers.Add(EnteteCle, cle);
        return await client.SendAsync(requete);
    }

    private static string CleDuKit(PostKitPanneResponse configuration)
    {
        using var json = JsonDocument.Parse(configuration.FichierAuthentification);
        return json.RootElement[0].GetProperty("ApiKey").GetString()!;
    }

    private static Guid IdCapteur(PostKitPanneResponse configuration, string propriete)
    {
        using var json = JsonDocument.Parse(configuration.FichierConfiguration);
        return Guid.Parse(json.RootElement.GetProperty(propriete).GetString()!);
    }
}
