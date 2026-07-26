using ErabliereAPI.Proxy;
using ErabliereApi.Mcp.Models;
using ErabliereApi.Mcp.Tools;
using ModelContextProtocol;
using NSubstitute;
using Shouldly;

namespace ErabliereApi.Mcp.Test;

/// <summary>
/// Behaviour of the list_capteurs and get_donnees_capteur tools.
/// </summary>
public class CapteurToolsTest
{
    private static readonly DateTimeOffset Origin = new(2026, 3, 12, 6, 0, 0, TimeSpan.FromHours(-4));

    private static Capteur CreateCapteur(Guid id, Guid idErabliere) => new()
    {
        Id = id,
        IdErabliere = idErabliere,
        Nom = "Température extérieure",
        Symbole = "°C",
        Type = "temperature",
        Online = true,
        BatteryLevel = 84,
        ReportFrequency = 300,
        LastMessageTime = Origin,
        // Navigation properties, never returned by the MCP tools.
        Erabliere = new Erabliere { Id = idErabliere, Nom = "Sucrerie du Nord" },
        DonneesCapteur = [new DonneeCapteur { Id = Guid.NewGuid() }]
    };

    private static IErabliereAPIProxy CreateProxy(
        Capteur? capteur = null,
        GetDonneesCapteurV2[]? donnees = null,
        Capteur[]? liste = null)
    {
        var proxy = Substitute.For<IErabliereAPIProxy>();

        proxy.CapteursAllAsync(
                 Arg.Any<Guid>(), ProxyArg.AnyString(), ProxyArg.AnyString(), ProxyArg.AnyString(),
                 ProxyArg.AnyInt(), ProxyArg.AnyInt(), ProxyArg.AnyBool(), ProxyArg.AnyString(), ProxyArg.AnyString(),
                 Arg.Any<CancellationToken>())
             .Returns(liste ?? []);

        if (capteur is not null)
        {
            proxy.CapteursGETAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                 .Returns(capteur);
        }

        proxy.DonneesCapteurV2AllAsync(
                 Arg.Any<Guid>(), Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset?>(), Arg.Any<DateTimeOffset?>(),
                 ProxyArg.AnyString(), ProxyArg.AnyInt(), Arg.Any<CancellationToken>())
             .Returns(donnees ?? []);

        return proxy;
    }

    private static GetDonneesCapteurV2[] Readings(int count)
    {
        return Enumerable.Range(0, count)
                         .Select(index => new GetDonneesCapteurV2
                         {
                             Id = Guid.NewGuid(),
                             D = Origin.AddMinutes(index * 5),
                             Valeur = index
                         })
                         .ToArray();
    }

    [Fact]
    public async Task ListCapteurs_ProjectsTheProxyDtoAndCountsTheOfflineOnes()
    {
        var idErabliere = Guid.NewGuid();
        var online = CreateCapteur(Guid.NewGuid(), idErabliere);
        var offline = CreateCapteur(Guid.NewGuid(), idErabliere);
        offline.Online = false;

        var proxy = CreateProxy(liste: [online, offline]);

        var response = await CapteurTools.ListCapteursAsync(proxy, idErabliere.ToString());

        response.Data.Count.ShouldBe(2);
        response.Data[0].Symbole.ShouldBe("°C");
        response.Data[0].BatteryLevel.ShouldBe(84);
        response.Summary.ShouldBe("2 sensors, 1 of them offline.");
        response.Truncated.ShouldBeFalse();
    }

    [Fact]
    public async Task ListCapteurs_WhenTheNameFilterIsGiven_ForwardsItAsTheDedicatedApiParameter()
    {
        var idErabliere = Guid.NewGuid();
        var proxy = CreateProxy();

        await CapteurTools.ListCapteursAsync(proxy, idErabliere.ToString(), nameContains: " Température ", top: 10);

        await proxy.Received(1).CapteursAllAsync(
            Arg.Is(idErabliere), ProxyArg.String("Température"), ProxyArg.NullString(), ProxyArg.NullString(),
            ProxyArg.Int(10), ProxyArg.NullInt(), ProxyArg.NullBool(), ProxyArg.NullString(), ProxyArg.String("indiceOrdre"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListCapteurs_WhenTheApiFillsTheRequestedTop_FlagsTheResponseAsTruncated()
    {
        var idErabliere = Guid.NewGuid();
        var proxy = CreateProxy(liste: [CreateCapteur(Guid.NewGuid(), idErabliere), CreateCapteur(Guid.NewGuid(), idErabliere)]);

        var response = await CapteurTools.ListCapteursAsync(proxy, idErabliere.ToString(), top: 2);

        response.Truncated.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-guid")]
    public async Task GetDonneesCapteur_WhenAnIdIsInvalid_ThrowsWithoutCallingTheApi(string capteurId)
    {
        var proxy = CreateProxy();

        await Should.ThrowAsync<McpException>(() => CapteurTools.GetDonneesCapteurAsync(
            proxy, Guid.NewGuid().ToString(), capteurId, "2026-03-12", "2026-03-13"));

        await proxy.DidNotReceiveWithAnyArgs().DonneesCapteurV2AllAsync(default, default, default, default, default, default, default);
    }

    [Theory]
    [InlineData(null, "2026-03-13")]
    [InlineData("2026-03-12", null)]
    [InlineData(null, null)]
    public async Task GetDonneesCapteur_WhenTheDateRangeIsIncomplete_ThrowsWithoutCallingTheApi(string? startDate, string? endDate)
    {
        var proxy = CreateProxy();

        var exception = await Should.ThrowAsync<McpException>(() => CapteurTools.GetDonneesCapteurAsync(
            proxy, Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), startDate!, endDate!));

        exception.Message.ShouldContain("both required");
        await proxy.DidNotReceiveWithAnyArgs().DonneesCapteurV2AllAsync(default, default, default, default, default, default, default);
    }

    [Fact]
    public async Task GetDonneesCapteur_WhenTheRangeIsInverted_ThrowsWithoutCallingTheApi()
    {
        var proxy = CreateProxy();

        var exception = await Should.ThrowAsync<McpException>(() => CapteurTools.GetDonneesCapteurAsync(
            proxy, Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "2026-03-20", "2026-03-12"));

        exception.Message.ShouldContain("must not be earlier than");
        await proxy.DidNotReceiveWithAnyArgs().DonneesCapteurV2AllAsync(default, default, default, default, default, default, default);
    }

    [Fact]
    public async Task GetDonneesCapteur_ForwardsTheRangeAndTheFetchCapToTheApi()
    {
        var idErabliere = Guid.NewGuid();
        var idCapteur = Guid.NewGuid();
        var proxy = CreateProxy(CreateCapteur(idCapteur, idErabliere), Readings(10));

        await CapteurTools.GetDonneesCapteurAsync(
            proxy, idErabliere.ToString(), idCapteur.ToString(), "2026-03-12T06:00:00-04:00", "2026-03-13T06:00:00-04:00");

        await proxy.Received(1).DonneesCapteurV2AllAsync(
            Arg.Is(idCapteur),
            Arg.Is<DateTimeOffset?>(value => value == null),
            Arg.Is<DateTimeOffset?>(value => value == new DateTimeOffset(2026, 3, 12, 6, 0, 0, TimeSpan.FromHours(-4))),
            Arg.Is<DateTimeOffset?>(value => value == new DateTimeOffset(2026, 3, 13, 6, 0, 0, TimeSpan.FromHours(-4))),
            ProxyArg.String("asc"),
            ProxyArg.Int(CapteurTools.MaxFetchedDonnees),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDonneesCapteur_TakesTheUnitFromTheSensor()
    {
        var idErabliere = Guid.NewGuid();
        var idCapteur = Guid.NewGuid();
        var capteur = CreateCapteur(idCapteur, idErabliere);
        capteur.Symbole = "Hg";

        var proxy = CreateProxy(capteur, Readings(10));

        var response = await CapteurTools.GetDonneesCapteurAsync(
            proxy, idErabliere.ToString(), idCapteur.ToString(), "2026-03-12", "2026-03-13");

        response.Data.Unit.ShouldBe("Hg");
        response.Summary.ShouldContain("Hg");
        response.Summary.ShouldContain("Température extérieure");
    }

    [Fact]
    public async Task GetDonneesCapteur_WhenTheApiFillsTheFetchCap_FlagsTheResponseAsTruncated()
    {
        var idErabliere = Guid.NewGuid();
        var idCapteur = Guid.NewGuid();
        var proxy = CreateProxy(CreateCapteur(idCapteur, idErabliere), Readings(CapteurTools.MaxFetchedDonnees));

        var response = await CapteurTools.GetDonneesCapteurAsync(
            proxy, idErabliere.ToString(), idCapteur.ToString(), "2026-03-12", "2026-04-13");

        response.Truncated.ShouldBeTrue();
        response.Summary.ShouldContain("narrow startDate and endDate");
        response.Data.Serie.Count.ShouldBe(100);
    }

    [Fact]
    public async Task GetDonneesCapteur_WhenTheRangeIsEmpty_ReturnsAnEmptySummaryRatherThanAnError()
    {
        var idErabliere = Guid.NewGuid();
        var idCapteur = Guid.NewGuid();
        var proxy = CreateProxy(CreateCapteur(idCapteur, idErabliere), []);

        var response = await CapteurTools.GetDonneesCapteurAsync(
            proxy, idErabliere.ToString(), idCapteur.ToString(), "2026-03-12", "2026-03-13");

        response.Data.Count.ShouldBe(0);
        response.Truncated.ShouldBeFalse();
        response.Summary.ShouldContain("no reading in the requested range");
    }

    [Fact]
    public async Task GetDonneesCapteur_WhenMaxPointsIsOutOfRange_ThrowsWithoutCallingTheApi()
    {
        var proxy = CreateProxy();

        var exception = await Should.ThrowAsync<McpException>(() => CapteurTools.GetDonneesCapteurAsync(
            proxy, Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "2026-03-12", "2026-03-13", maxPoints: 5000));

        exception.Message.ShouldContain("between 1 and");
        await proxy.DidNotReceiveWithAnyArgs().DonneesCapteurV2AllAsync(default, default, default, default, default, default, default);
    }

    [Fact]
    public async Task GetDonneesCapteur_WhenTheSensorIsUnknown_ThrowsAnActionableMcpException()
    {
        var idErabliere = Guid.NewGuid();
        var idCapteur = Guid.NewGuid();
        var proxy = CreateProxy();

        proxy.CapteursGETAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
             .Returns<Capteur>(_ => throw new ApiException("Not Found", 404, null, new Dictionary<string, IEnumerable<string>>(), null));

        var exception = await Should.ThrowAsync<McpException>(() => CapteurTools.GetDonneesCapteurAsync(
            proxy, idErabliere.ToString(), idCapteur.ToString(), "2026-03-12", "2026-03-13"));

        exception.Message.ShouldContain(idCapteur.ToString());
        exception.Message.ShouldContain("list_capteurs");
        await proxy.DidNotReceiveWithAnyArgs().DonneesCapteurV2AllAsync(default, default, default, default, default, default, default);
    }

    [Fact]
    public async Task GetDonneesCapteur_OverAMonthOfReadings_StaysUnderTheResponseBudget()
    {
        var idErabliere = Guid.NewGuid();
        var idCapteur = Guid.NewGuid();
        var proxy = CreateProxy(CreateCapteur(idCapteur, idErabliere), Readings(CapteurTools.MaxFetchedDonnees));

        var response = await CapteurTools.GetDonneesCapteurAsync(
            proxy, idErabliere.ToString(), idCapteur.ToString(), "2026-03-12", "2026-04-13", maxPoints: 200);

        ToolResponse.EstimateTokens(response).ShouldBeLessThan(ToolResponse.MaxResponseTokens);
    }
}
