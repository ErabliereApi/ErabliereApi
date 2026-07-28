using ErabliereAPI.Proxy;
using ErabliereApi.Mcp.Tools;
using ModelContextProtocol;
using NSubstitute;
using Shouldly;

namespace ErabliereApi.Mcp.Test;

/// <summary>
/// Behaviour of the get_alertes tool.
/// </summary>
public class AlerteToolsTest
{
    private static Alerte CreateAlerte(Guid idErabliere) => new()
    {
        Id = Guid.NewGuid(),
        IdErabliere = idErabliere,
        Nom = "Température trop basse",
        IsEnable = true,
        EnvoyerA = "producteur@example.com",
        TemperatureThresholdLow = "-5",
        TemperatureThresholdHight = "25",
        LastOccurence = new DateTimeOffset(2026, 3, 12, 6, 30, 0, TimeSpan.FromHours(-4)),
        // Navigation property, never returned by the MCP tools.
        Erabliere = new Erabliere { Id = idErabliere, Nom = "Sucrerie du Nord" }
    };

    private static IErabliereAPIProxy CreateProxy(params Alerte[] result)
    {
        var proxy = Substitute.For<IErabliereAPIProxy>();

        proxy.AlertesAllAsync(
                 Arg.Any<Guid>(),
                 ProxyArg.AnyString(), ProxyArg.AnyString(), ProxyArg.AnyInt(), ProxyArg.AnyInt(),
                 ProxyArg.AnyBool(), ProxyArg.AnyString(), ProxyArg.AnyString(),
                 Arg.Any<CancellationToken>())
             .Returns(result);

        return proxy;
    }

    private static AlerteCapteur CreateAlerteCapteur(string capteurNom, string nom, double? min = null, double? max = null, bool isEnable = true) => new()
    {
        Id = Guid.NewGuid(),
        IdCapteur = Guid.NewGuid(),
        Nom = nom,
        IsEnable = isEnable,
        EnvoyerA = "producteur@example.com",
        MinValue = min,
        MaxValue = max,
        LastOccurence = new DateTimeOffset(2026, 3, 12, 6, 30, 0, TimeSpan.FromHours(-4)),
        // Navigation property, included on the call, projected but never returned as-is.
        Capteur = new Capteur { Id = Guid.NewGuid(), Nom = capteurNom, Symbole = "°C" }
    };

    private static IErabliereAPIProxy CreateCapteurAlerteProxy(params AlerteCapteur[] result)
    {
        var proxy = Substitute.For<IErabliereAPIProxy>();

        proxy.AlertesCapteurAsync(Arg.Any<Guid>(), ProxyArg.AnyString(), Arg.Any<CancellationToken>())
             .Returns(result);

        return proxy;
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-guid")]
    public async Task GetAlertes_WhenIdIsInvalid_ThrowsWithoutCallingTheApi(string erabliereId)
    {
        var proxy = CreateProxy();

        await Should.ThrowAsync<McpException>(() => AlerteTools.GetAlertesAsync(proxy, erabliereId));

        await proxy.DidNotReceiveWithAnyArgs().AlertesAllAsync(
            default, default, default, default, default, default, default, default, default);
    }

    [Fact]
    public async Task GetAlertes_WhenTopIsOutOfRange_ThrowsWithoutCallingTheApi()
    {
        var proxy = CreateProxy();

        var exception = await Should.ThrowAsync<McpException>(
            () => AlerteTools.GetAlertesAsync(proxy, Guid.NewGuid().ToString(), top: 500));

        exception.Message.ShouldContain("between 1 and");
        await proxy.DidNotReceiveWithAnyArgs().AlertesAllAsync(
            default, default, default, default, default, default, default, default, default);
    }

    [Fact]
    public async Task GetAlertes_ForwardsTheIdentifierAndTheDefaultTop()
    {
        var id = Guid.NewGuid();
        var proxy = CreateProxy(CreateAlerte(id));

        var result = await AlerteTools.GetAlertesAsync(proxy, id.ToString());

        result.Data.Count.ShouldBe(1);
        await proxy.Received(1).AlertesAllAsync(
            Arg.Is(id),
            ProxyArg.NullString(), ProxyArg.NullString(), ProxyArg.Int(ToolArguments.DefaultTop), ProxyArg.NullInt(),
            ProxyArg.NullBool(), ProxyArg.NullString(), ProxyArg.NullString(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAlertes_ProjectsTheProxyDtoOnTheSummary()
    {
        var id = Guid.NewGuid();
        var alerte = CreateAlerte(id);
        var proxy = CreateProxy(alerte);

        var summary = (await AlerteTools.GetAlertesAsync(proxy, id.ToString())).Data.Single();

        summary.Id.ShouldBe(alerte.Id);
        summary.IdErabliere.ShouldBe(id);
        summary.Nom.ShouldBe("Température trop basse");
        summary.IsEnable.ShouldBe(true);
        summary.EnvoyerA.ShouldBe("producteur@example.com");
        summary.TemperatureThresholdLow.ShouldBe("-5");
        summary.TemperatureThresholdHight.ShouldBe("25");
        summary.LastOccurence.ShouldBe(alerte.LastOccurence);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-guid")]
    public async Task GetAlertesCapteur_WhenIdIsInvalid_ThrowsWithoutCallingTheApi(string erabliereId)
    {
        var proxy = CreateCapteurAlerteProxy();

        await Should.ThrowAsync<McpException>(() => AlerteTools.GetAlertesCapteurAsync(proxy, erabliereId));

        await proxy.DidNotReceiveWithAnyArgs().AlertesCapteurAsync(default, default, default);
    }

    [Fact]
    public async Task GetAlertesCapteur_WhenTopIsOutOfRange_ThrowsWithoutCallingTheApi()
    {
        var proxy = CreateCapteurAlerteProxy();

        var exception = await Should.ThrowAsync<McpException>(
            () => AlerteTools.GetAlertesCapteurAsync(proxy, Guid.NewGuid().ToString(), top: 0));

        exception.Message.ShouldContain("between 1 and");
        await proxy.DidNotReceiveWithAnyArgs().AlertesCapteurAsync(default, default, default);
    }

    [Fact]
    public async Task GetAlertesCapteur_AsksTheApiToIncludeTheSensor()
    {
        // Without the sensor, a bound of 12 carries neither a name nor a unit.
        var id = Guid.NewGuid();
        var proxy = CreateCapteurAlerteProxy(CreateAlerteCapteur("Température extérieure", "Gel"));

        await AlerteTools.GetAlertesCapteurAsync(proxy, id.ToString());

        await proxy.Received(1).AlertesCapteurAsync(Arg.Is(id), ProxyArg.String("Capteur"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAlertesCapteur_ProjectsTheProxyDtoOnTheSummary()
    {
        var id = Guid.NewGuid();
        var alerte = CreateAlerteCapteur("Température extérieure", "Gel", min: -2.5, max: 24);
        var proxy = CreateCapteurAlerteProxy(alerte);

        var summary = (await AlerteTools.GetAlertesCapteurAsync(proxy, id.ToString())).Data.Single();

        summary.Id.ShouldBe(alerte.Id);
        summary.IdCapteur.ShouldBe(alerte.IdCapteur);
        summary.CapteurNom.ShouldBe("Température extérieure");
        summary.CapteurSymbole.ShouldBe("°C");
        summary.Nom.ShouldBe("Gel");
        summary.IsEnable.ShouldBe(true);
        summary.EnvoyerA.ShouldBe("producteur@example.com");
        summary.MinValue.ShouldBe(-2.5);
        summary.MaxValue.ShouldBe(24);
        summary.LastOccurence.ShouldBe(alerte.LastOccurence);
    }

    [Fact]
    public async Task GetAlertesCapteur_GroupsTheAlertsBySensor()
    {
        // The endpoint orders nothing, so the tool does: the cut-off tail has to be
        // the same one on two identical calls.
        var proxy = CreateCapteurAlerteProxy(
            CreateAlerteCapteur("Vacuum secteur 2", "Perte de vide"),
            CreateAlerteCapteur("Température extérieure", "Redoux"),
            CreateAlerteCapteur("Température extérieure", "Gel"));

        var result = await AlerteTools.GetAlertesCapteurAsync(proxy, Guid.NewGuid().ToString());

        result.Data.Select(alerte => $"{alerte.CapteurNom} / {alerte.Nom}")
              .ShouldBe([
                  "Température extérieure / Gel",
                  "Température extérieure / Redoux",
                  "Vacuum secteur 2 / Perte de vide"
              ]);
    }

    [Fact]
    public async Task GetAlertesCapteur_WhenTheApiReturnsMoreThanTop_CutsTheListAndSaysSo()
    {
        // The route takes no OData argument, so the cap is applied here rather than
        // by the API, and the model has to be told the answer is partial.
        var proxy = CreateCapteurAlerteProxy(
            CreateAlerteCapteur("Capteur A", "Gel"),
            CreateAlerteCapteur("Capteur B", "Gel"),
            CreateAlerteCapteur("Capteur C", "Gel"));

        var result = await AlerteTools.GetAlertesCapteurAsync(proxy, Guid.NewGuid().ToString(), top: 2);

        result.Data.Count.ShouldBe(2);
        result.Truncated.ShouldBeTrue();
        result.Summary.ShouldContain("3 sensor alerts in total");
    }

    [Fact]
    public async Task GetAlertesCapteur_SummarizesTheEnabledAlertsAndTheWatchedSensors()
    {
        var proxy = CreateCapteurAlerteProxy(
            CreateAlerteCapteur("Température extérieure", "Gel"),
            CreateAlerteCapteur("Vacuum secteur 2", "Perte de vide", isEnable: false));

        var result = await AlerteTools.GetAlertesCapteurAsync(proxy, Guid.NewGuid().ToString());

        result.Summary.ShouldBe("2 sensor alerts on 2 sensors, 1 of them enabled.");
        result.Truncated.ShouldBeFalse();
    }

    [Fact]
    public async Task GetAlertesCapteur_WhenNoSensorIsWatched_SaysSo()
    {
        var proxy = CreateCapteurAlerteProxy();

        var result = await AlerteTools.GetAlertesCapteurAsync(proxy, Guid.NewGuid().ToString());

        result.Data.ShouldBeEmpty();
        result.Summary.ShouldBe("No sensor of this maple grove has a configured alert.");
        result.Truncated.ShouldBeFalse();
    }
}
