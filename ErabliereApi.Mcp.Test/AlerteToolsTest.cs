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
}
