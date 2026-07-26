using ErabliereAPI.Proxy;
using ErabliereApi.Mcp.Tools;
using ModelContextProtocol;
using NSubstitute;
using Shouldly;

namespace ErabliereApi.Mcp.Test;

/// <summary>
/// Behaviour of the list_erablieres and get_erabliere tools.
/// </summary>
public class ErabliereToolsTest
{
    private static Erabliere CreateErabliere(Guid id, string nom = "Sucrerie du Nord") => new()
    {
        Id = id,
        Nom = nom,
        Description = "Une érablière de test",
        Addresse = "1 rue des Érables",
        CodePostal = "G0A 1A0",
        RegionAdministrative = "Capitale-Nationale",
        IsPublic = true,
        IndiceOrdre = 3,
        // Navigation property, never returned by the MCP tools.
        Capteurs = [new Capteur { Id = Guid.NewGuid() }]
    };

    private static IErabliereAPIProxy CreateProxy(params Erabliere[] result)
    {
        var proxy = Substitute.For<IErabliereAPIProxy>();

        proxy.ErablieresAllAsync(
                 ProxyArg.AnyString(), ProxyArg.AnyString(), ProxyArg.AnyString(),
                 ProxyArg.AnyInt(), ProxyArg.AnyInt(), ProxyArg.AnyBool(), ProxyArg.AnyString(),
                 Arg.Any<CancellationToken>())
             .Returns(result);

        return proxy;
    }

    [Fact]
    public async Task ListErablieres_WhenNoArgument_UsesTheDefaultTopAndNoFilter()
    {
        var proxy = CreateProxy(CreateErabliere(Guid.NewGuid()));

        var result = await ErabliereTools.ListErablieresAsync(proxy);

        result.Data.Count.ShouldBe(1);
        await proxy.Received(1).ErablieresAllAsync(
            ProxyArg.NullString(), ProxyArg.NullString(), ProxyArg.NullString(),
            ProxyArg.Int(ToolArguments.DefaultTop), ProxyArg.NullInt(), ProxyArg.NullBool(), ProxyArg.NullString(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListErablieres_WhenNameContains_ForwardsAnODataFilter()
    {
        var proxy = CreateProxy();

        await ErabliereTools.ListErablieresAsync(proxy, nameContains: "Nord", top: 10);

        await proxy.Received(1).ErablieresAllAsync(
            ProxyArg.NullString(), ProxyArg.NullString(), ProxyArg.String("contains(nom,'Nord')"),
            ProxyArg.Int(10), ProxyArg.NullInt(), ProxyArg.NullBool(), ProxyArg.NullString(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListErablieres_ProjectsTheProxyDtoOnTheSummary()
    {
        var id = Guid.NewGuid();
        var proxy = CreateProxy(CreateErabliere(id));

        var summary = (await ErabliereTools.ListErablieresAsync(proxy)).Data.Single();

        summary.Id.ShouldBe(id);
        summary.Nom.ShouldBe("Sucrerie du Nord");
        summary.Description.ShouldBe("Une érablière de test");
        summary.Addresse.ShouldBe("1 rue des Érables");
        summary.CodePostal.ShouldBe("G0A 1A0");
        summary.RegionAdministrative.ShouldBe("Capitale-Nationale");
        summary.IsPublic.ShouldBe(true);
        summary.IndiceOrdre.ShouldBe(3);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(ToolArguments.MaxTop + 1)]
    public async Task ListErablieres_WhenTopIsOutOfRange_ThrowsWithoutCallingTheApi(int top)
    {
        var proxy = CreateProxy();

        var exception = await Should.ThrowAsync<McpException>(() => ErabliereTools.ListErablieresAsync(proxy, top: top));

        exception.Message.ShouldContain("between 1 and");
        await proxy.DidNotReceiveWithAnyArgs().ErablieresAllAsync(
            default, default, default, default, default, default, default, default);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-guid")]
    public async Task GetErabliere_WhenIdIsInvalid_ThrowsWithoutCallingTheApi(string erabliereId)
    {
        var proxy = CreateProxy();

        await Should.ThrowAsync<McpException>(() => ErabliereTools.GetErabliereAsync(proxy, erabliereId));

        await proxy.DidNotReceiveWithAnyArgs().ErablieresAllAsync(
            default, default, default, default, default, default, default, default);
    }

    [Fact]
    public async Task GetErabliere_FiltersOnTheIdentifier()
    {
        var id = Guid.NewGuid();
        var proxy = CreateProxy(CreateErabliere(id));

        var result = await ErabliereTools.GetErabliereAsync(proxy, id.ToString());

        result.Data.Id.ShouldBe(id);
        await proxy.Received(1).ErablieresAllAsync(
            ProxyArg.NullString(), ProxyArg.NullString(), ProxyArg.String($"id eq {id}"),
            ProxyArg.Int(1), ProxyArg.NullInt(), ProxyArg.NullBool(), ProxyArg.NullString(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetErabliere_WhenTheApiReturnsNothing_ThrowsAnActionableMcpException()
    {
        var id = Guid.NewGuid();
        var proxy = CreateProxy();

        var exception = await Should.ThrowAsync<McpException>(() => ErabliereTools.GetErabliereAsync(proxy, id.ToString()));

        exception.Message.ShouldContain(id.ToString());
        exception.Message.ShouldContain("list_erablieres");
    }
}
