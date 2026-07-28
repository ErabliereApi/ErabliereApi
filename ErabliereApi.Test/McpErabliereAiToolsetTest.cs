using ErabliereAPI.Proxy;
using ErabliereApi.Services.AI;
using ErabliereApi.Services.AI.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ErabliereApi.Test;

/// <summary>
/// L'exécution d'un appel d'outil demandé par le modèle.
///
/// Le principe : un appel qui échoue est une donnée pour le modèle, pas une requête
/// ratée. Tout ce qu'il pourrait corriger lui-même — mauvais identifiant, outil
/// inexistant, plage trop large — lui revient sous forme de résultat lisible, pour
/// qu'il se reprenne au tour suivant au lieu de faire échouer la conversation.
/// </summary>
public class McpErabliereAiToolsetTest
{
    private static (IErabliereAiToolset toolset, IErabliereAPIProxy proxy) CreateToolset(
        Action<ErabliereAiToolOptions>? configure = null)
    {
        var options = new ErabliereAiToolOptions();
        configure?.Invoke(options);

        var proxy = Substitute.For<IErabliereAPIProxy>();

        var services = new ServiceCollection();
        services.AddSingleton(proxy);

        var toolset = new McpErabliereAiToolset(
            new ErabliereAiToolCatalog(Options.Create(options)),
            services.BuildServiceProvider(),
            Options.Create(options),
            NullLogger<McpErabliereAiToolset>.Instance);

        return (toolset, proxy);
    }

    private static string ErrorOf(ErabliereAiToolResult result)
    {
        return JsonDocument.Parse(result.ResultJson).RootElement.GetProperty("error").GetString() ?? "";
    }

    [Fact]
    public async Task InvokeAsync_OutilInconnu_RetourneUneErreurQuiListeLesOutils()
    {
        var (toolset, _) = CreateToolset();

        var result = await toolset.InvokeAsync(
            new AIToolCall("call_1", "supprimer_erabliere", "{}"), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Contains("list_erablieres", ErrorOf(result));
    }

    [Fact]
    public async Task InvokeAsync_ArgumentsIllisibles_RetourneUneErreurPlutotQueLever()
    {
        var (toolset, _) = CreateToolset();

        var result = await toolset.InvokeAsync(
            new AIToolCall("call_1", "list_erablieres", "ceci n'est pas du json"), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Contains("json", ErrorOf(result), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InvokeAsync_IdentifiantInvalide_RemonteLeMessageEcritPourLeModele()
    {
        var (toolset, _) = CreateToolset();

        var result = await toolset.InvokeAsync(
            new AIToolCall("call_1", "get_erabliere", """{"erabliereId":"pas-un-guid"}"""),
            CancellationToken.None);

        Assert.True(result.IsError);

        // Le message des outils MCP dit quoi faire, contrairement à une trace de pile.
        Assert.Contains("GUID", ErrorOf(result));
    }

    [Fact]
    public async Task InvokeAsync_AppelReussi_RetourneLEnveloppeDeLOutil()
    {
        var (toolset, proxy) = CreateToolset();

        proxy.ErablieresAllAsync(
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<int?>(),
                Arg.Any<int?>(), Arg.Any<bool?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
             .Returns(new List<Erabliere> { new() { Id = Guid.NewGuid(), Nom = "Sucrerie du Nord" } });

        var result = await toolset.InvokeAsync(
            new AIToolCall("call_1", "list_erablieres", "{}"), CancellationToken.None);

        Assert.False(result.IsError);

        var envelope = JsonDocument.Parse(result.ResultJson).RootElement;

        Assert.Contains("Sucrerie du Nord", envelope.GetProperty("summary").GetString());
        Assert.False(envelope.GetProperty("truncated").GetBoolean());
        Assert.True(result.EstimatedTokens > 0);
    }

    [Fact]
    public async Task InvokeAsync_OutilTropLent_AbandonneEtDitCommentSeReprendre()
    {
        var (toolset, proxy) = CreateToolset(options => options.ToolTimeout = TimeSpan.FromMilliseconds(50));

        proxy.ErablieresAllAsync(
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<int?>(),
                Arg.Any<int?>(), Arg.Any<bool?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
             .Returns(callInfo => NeverCompletesAsync(callInfo.Arg<CancellationToken>()));

        var result = await toolset.InvokeAsync(
            new AIToolCall("call_1", "list_erablieres", "{}"), CancellationToken.None);

        Assert.True(result.IsError);
        Assert.Contains("abandoned", ErrorOf(result));
    }

    [Fact]
    public async Task InvokeAsync_ErreurInattendue_NeDivulguePasLeDetail()
    {
        var (toolset, proxy) = CreateToolset();

        proxy.ErablieresAllAsync(
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<int?>(),
                Arg.Any<int?>(), Arg.Any<bool?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
             .Returns(_ => FailsAsync(new InvalidOperationException("Server=prod;Password=secret")));

        var result = await toolset.InvokeAsync(
            new AIToolCall("call_1", "list_erablieres", "{}"), CancellationToken.None);

        Assert.True(result.IsError);

        // Le message d'une exception inattendue peut porter une chaîne de connexion;
        // il ne doit jamais atteindre le modèle, donc l'utilisateur.
        Assert.DoesNotContain("Password", result.ResultJson);
        Assert.DoesNotContain("prod", result.ResultJson);
    }

    [Fact]
    public async Task InvokeAsync_AnnulationDeLaRequete_RemonteLAnnulation()
    {
        var (toolset, proxy) = CreateToolset();

        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        proxy.ErablieresAllAsync(
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<int?>(),
                Arg.Any<int?>(), Arg.Any<bool?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
             .Returns(callInfo => NeverCompletesAsync(callInfo.Arg<CancellationToken>()));

        // L'utilisateur a fermé l'onglet : plus personne n'attend de réponse, donc
        // l'annulation ne doit pas être maquillée en résultat d'outil en erreur.
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            toolset.InvokeAsync(new AIToolCall("call_1", "list_erablieres", "{}"), cancellation.Token));
    }

    private static async Task<ICollection<Erabliere>> NeverCompletesAsync(CancellationToken token)
    {
        await Task.Delay(Timeout.Infinite, token);

        return [];
    }

    private static async Task<ICollection<Erabliere>> FailsAsync(Exception exception)
    {
        await Task.Yield();

        throw exception;
    }
}
