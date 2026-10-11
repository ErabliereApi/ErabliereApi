using ErabliereApi.Integration.Test.ApplicationFactory;
using Shouldly;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace ErabliereApi.Integration.Test;

public class SecurityHeadersTest : IClassFixture<ErabliereApiApplicationFactory<Startup>>
{
    private readonly ErabliereApiApplicationFactory<Startup> _factory;

    public SecurityHeadersTest(ErabliereApiApplicationFactory<Startup> factory)
    {
        _factory = factory;
    }

    // Un contrôleur, un endpoint hors contrôleur et une route inconnue : les réponses
    // des contrôleurs sortaient sans entêtes quand le middleware suivait UseEndpoints.
    [Theory]
    [InlineData("/Erablieres")]
    [InlineData("/live")]
    [InlineData("/api/route-inexistante")]
    public async Task LesEntetesDeSecuriteSontPresentsSurToutesLesReponses(string url)
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(url);

        Entete(response, "Content-Security-Policy").ShouldContain("default-src 'self'");
        Entete(response, "X-Frame-Options").ShouldBe("SAMEORIGIN");
        Entete(response, "X-Content-Type-Options").ShouldBe("nosniff");
        Entete(response, "X-XSS-Protection").ShouldBe("0");
        Entete(response, "Referrer-Policy").ShouldBe("strict-origin-when-cross-origin");
        Entete(response, "Permissions-Policy").ShouldContain("geolocation=(self)");
        Entete(response, "Cross-Origin-Opener-Policy").ShouldBe("same-origin-allow-popups");
        Entete(response, "Cross-Origin-Resource-Policy").ShouldBe("cross-origin");
    }

    private static string Entete(HttpResponseMessage response, string nom)
    {
        response.Headers.TryGetValues(nom, out var valeurs)
            .ShouldBeTrue($"L'entête {nom} est absent de {response.RequestMessage?.RequestUri}");

        // Une seule valeur : un entête ajouté deux fois dans le pipeline sortirait en double.
        return valeurs!.ShouldHaveSingleItem();
    }
}
