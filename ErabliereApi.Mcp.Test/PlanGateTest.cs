using System.Net;
using System.Text;
using System.Text.Json;
using ErabliereAPI.Proxy;
using ErabliereApi.Mcp.Configuration;
using ErabliereApi.Mcp.Extensions;
using ErabliereApi.Mcp.Hosting;
using ErabliereApi.Mcp.Http;
using ErabliereApi.Mcp.Services;
using ErabliereApi.Mcp.Tools;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;
using Shouldly;

namespace ErabliereApi.Mcp.Test;

/// <summary>
/// Drives the plan gate through the real HTTP pipeline of
/// <see cref="HttpServerRunner"/>, against a stand-in ErabliereAPI answering
/// <c>GET /api/Abonnements/Courant</c>.
/// </summary>
/// <remarks>
/// The gate is asserted where it matters: on the wire. A caller on the wrong plan
/// must get a JSON-RPC error carrying a sentence they can act on, not a status code
/// their MCP client turns into "the server returned 403".
/// </remarks>
public class PlanGateTest
{
    private const string TestApiKey = "an-api-key";

    /// <summary>
    /// Stands in for ErabliereAPI: answers the subscription endpoint with the plan
    /// the test asked for, and everything else with an empty list.
    /// </summary>
    private sealed class FakeErabliereApi : HttpMessageHandler
    {
        public HttpStatusCode PlanStatusCode { get; set; } = HttpStatusCode.OK;

        public string PlanBody { get; set; } = PlanJson("base", abonnementActif: true);

        public int PlanRequestCount { get; private set; }

        public bool Unreachable { get; set; }

        public static string PlanJson(string plan, bool abonnementActif, string? frequenceFacturation = null)
        {
            return JsonSerializer.Serialize(new
            {
                plan,
                abonnementActif,
                dateDebut = abonnementActif ? "2026-01-01T00:00:00-05:00" : null,
                dateFin = (string?)null,
                frequenceFacturation
            });
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (Unreachable)
            {
                throw new HttpRequestException("The stand-in ErabliereAPI is down.");
            }

            if (request.RequestUri?.AbsolutePath == ErabliereApiSubscriptionPlanResolver.PlanPath)
            {
                PlanRequestCount++;

                return Task.FromResult(new HttpResponseMessage(PlanStatusCode)
                {
                    Content = new StringContent(PlanBody, Encoding.UTF8, "application/json")
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]", Encoding.UTF8, "application/json")
            });
        }
    }

    /// <summary>
    /// The gating section as an operator would write it: the base plan opens MCP,
    /// the free one does not.
    /// </summary>
    private static Dictionary<string, string?> GatingConfiguration(bool enabled, string? cacheDuration = "00:00:00")
    {
        return new Dictionary<string, string?>
        {
            [ErabliereApiMcpOptions.BaseUrlEnvironmentVariable] = "https://erabliereapi.test",
            ["Mcp:PlanGating:Enabled"] = enabled ? "true" : "false",
            ["Mcp:PlanGating:RequiredCapability"] = "mcp",
            // Off by default in the tests: each one wants its own answer from the
            // stand-in API rather than the one a previous request warmed.
            ["Mcp:PlanGating:CacheDuration"] = cacheDuration,
            ["Mcp:PlanGating:SubscriptionUrl"] = "https://erabliereapi.test/abonnement",
            ["Mcp:PlanGating:PlanCapabilities:gratuit:0"] = "",
            ["Mcp:PlanGating:PlanCapabilities:base:0"] = "mcp"
        };
    }

    private static async Task<WebApplication> StartServerAsync(FakeErabliereApi erabliereApi, Dictionary<string, string?> configuration)
    {
        var builder = WebApplication.CreateBuilder();

        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(configuration);

        HttpServerRunner.ConfigureServices(builder);

        builder.Services.AddHttpClient(ErabliereApiMcpOptions.HttpClientName)
                        .ConfigurePrimaryHttpMessageHandler(() => erabliereApi);

        var app = builder.Build();

        HttpServerRunner.MapEndpoints(app);

        await app.StartAsync();

        return app;
    }

    private static HttpClient CreateHttpClient(WebApplication app, string? apiKey = TestApiKey)
    {
        var httpClient = app.GetTestClient();

        if (apiKey is not null)
        {
            httpClient.DefaultRequestHeaders.Add(ApiKeyHandler.XApiKeyHeader, apiKey);
        }

        // Both media types, as the Streamable HTTP transport requires: without them
        // the MCP endpoint answers 406 before ever running, and a request that is let
        // through would look like a failure for the wrong reason.
        httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/json, text/event-stream");

        return httpClient;
    }

    private static Task<HttpResponseMessage> ListToolsAsync(HttpClient httpClient)
    {
        return httpClient.PostAsync(
            HttpServerRunner.McpPath,
            new StringContent("""{"jsonrpc":"2.0","id":7,"method":"tools/list"}""", Encoding.UTF8, "application/json"));
    }

    /// <summary>
    /// Reads the JSON-RPC message of an answer, whichever way the endpoint chose to
    /// send it: the refusals are written as plain JSON, while a request let through
    /// comes back on the SSE channel of the Streamable HTTP transport.
    /// </summary>
    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();

        if (response.Content.Headers.ContentType?.MediaType == "text/event-stream")
        {
            body = body.Split('\n')
                       .First(line => line.StartsWith("data:", StringComparison.Ordinal))
                       ["data:".Length..];
        }

        using var document = JsonDocument.Parse(body);

        return document.RootElement.Clone();
    }

    #region The gate

    [Fact]
    public async Task APlanGrantingMcpAccessGoesThrough()
    {
        var erabliereApi = new FakeErabliereApi { PlanBody = FakeErabliereApi.PlanJson("base", abonnementActif: true, "mensuelle") };

        await using var app = await StartServerAsync(erabliereApi, GatingConfiguration(enabled: true));
        using var httpClient = CreateHttpClient(app);

        using var response = await ListToolsAsync(httpClient);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await ReadJsonAsync(response);

        payload.TryGetProperty("error", out _).ShouldBeFalse();
        payload.GetProperty("result").GetProperty("tools").GetArrayLength().ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task APlanWithoutMcpAccessIsRefusedWithAJsonRpcError()
    {
        var erabliereApi = new FakeErabliereApi { PlanBody = FakeErabliereApi.PlanJson("gratuit", abonnementActif: true) };

        await using var app = await StartServerAsync(erabliereApi, GatingConfiguration(enabled: true));
        using var httpClient = CreateHttpClient(app);

        using var response = await ListToolsAsync(httpClient);

        // 200 and not 403: an MCP client turns a non-2xx into a transport failure and
        // usually drops the body, which is exactly the sentence the user needs.
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var payload = await ReadJsonAsync(response);

        payload.GetProperty("jsonrpc").GetString().ShouldBe("2.0");
        payload.GetProperty("id").GetInt32().ShouldBe(7);

        var error = payload.GetProperty("error");

        error.GetProperty("code").GetInt32().ShouldBe(JsonRpcErrorWriter.PlanRequiredErrorCode);

        var message = error.GetProperty("message").GetString()!;

        message.ShouldContain("gratuit");
        message.ShouldContain("base");
        message.ShouldContain("https://erabliereapi.test/abonnement");

        var data = error.GetProperty("data");

        data.GetProperty("currentPlan").GetString().ShouldBe("gratuit");
        data.GetProperty("requiredCapability").GetString().ShouldBe("mcp");
        data.GetProperty("plansGrantingAccess").GetString().ShouldBe("base");
    }

    [Fact]
    public async Task AnAccountWithoutAnySubscriptionIsRefusedAndToldSo()
    {
        // The most common denial: never subscribed at all. The message has to say
        // that, not "your plan is wrong".
        var erabliereApi = new FakeErabliereApi { PlanBody = FakeErabliereApi.PlanJson("gratuit", abonnementActif: false) };

        await using var app = await StartServerAsync(erabliereApi, GatingConfiguration(enabled: true));
        using var httpClient = CreateHttpClient(app);

        using var response = await ListToolsAsync(httpClient);

        var error = (await ReadJsonAsync(response)).GetProperty("error");

        error.GetProperty("code").GetInt32().ShouldBe(JsonRpcErrorWriter.PlanRequiredErrorCode);
        error.GetProperty("message").GetString()!.ShouldContain("no active subscription");
    }

    [Fact]
    public async Task TheDenialReasonIsAlsoOnAResponseHeaderForTheOperator()
    {
        var erabliereApi = new FakeErabliereApi { PlanBody = FakeErabliereApi.PlanJson("gratuit", abonnementActif: false) };

        await using var app = await StartServerAsync(erabliereApi, GatingConfiguration(enabled: true));
        using var httpClient = CreateHttpClient(app);

        using var response = await ListToolsAsync(httpClient);

        response.Headers.GetValues(JsonRpcErrorWriter.DeniedReasonHeader)
                .ShouldHaveSingleItem()
                .ShouldContain("does not include access");
    }

    [Fact]
    public async Task InitializeIsGatedToo()
    {
        // Not only the tool calls: a plan that opens nothing should not even read the
        // tool catalog, which names the features of the account.
        var erabliereApi = new FakeErabliereApi { PlanBody = FakeErabliereApi.PlanJson("gratuit", abonnementActif: false) };

        await using var app = await StartServerAsync(erabliereApi, GatingConfiguration(enabled: true));
        using var httpClient = CreateHttpClient(app);

        using var response = await httpClient.PostAsync(
            HttpServerRunner.McpPath,
            new StringContent(
                """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}""",
                Encoding.UTF8,
                "application/json"));

        (await ReadJsonAsync(response)).GetProperty("error")
                                       .GetProperty("code")
                                       .GetInt32()
                                       .ShouldBe(JsonRpcErrorWriter.PlanRequiredErrorCode);
    }

    [Fact]
    public async Task ARealMcpClientSurfacesTheDenialMessage()
    {
        // The point of answering a JSON-RPC error rather than a status code: the
        // sentence has to reach whoever is on the other end of the client.
        var erabliereApi = new FakeErabliereApi { PlanBody = FakeErabliereApi.PlanJson("gratuit", abonnementActif: false) };

        await using var app = await StartServerAsync(erabliereApi, GatingConfiguration(enabled: true));

        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri($"http://localhost{HttpServerRunner.McpPath}"),
                TransportMode = HttpTransportMode.StreamableHttp
            },
            CreateHttpClient(app),
            loggerFactory: null,
            ownsHttpClient: true);

        var exception = await Should.ThrowAsync<Exception>(async () => await McpClient.CreateAsync(transport));

        exception.ToString().ShouldContain("does not include access to the ErabliereAPI MCP server");
    }

    [Fact]
    public async Task TheApiKeyCheckStillRunsFirst()
    {
        // A request without a key must not cost a call to ErabliereAPI.
        var erabliereApi = new FakeErabliereApi();

        await using var app = await StartServerAsync(erabliereApi, GatingConfiguration(enabled: true));
        using var httpClient = CreateHttpClient(app, apiKey: null);

        using var response = await ListToolsAsync(httpClient);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        erabliereApi.PlanRequestCount.ShouldBe(0);
    }

    #endregion

    #region The gate turned off

    [Fact]
    public async Task WithTheGateOffEveryPlanGoesThroughAndNoPlanIsEvenRead()
    {
        // What an existing deployment gets after the upgrade: nothing changes, and
        // not one extra call to ErabliereAPI.
        var erabliereApi = new FakeErabliereApi { PlanBody = FakeErabliereApi.PlanJson("gratuit", abonnementActif: false) };

        await using var app = await StartServerAsync(erabliereApi, GatingConfiguration(enabled: false));
        using var httpClient = CreateHttpClient(app);

        using var response = await ListToolsAsync(httpClient);

        (await ReadJsonAsync(response)).TryGetProperty("error", out _).ShouldBeFalse();
        erabliereApi.PlanRequestCount.ShouldBe(0);
    }

    #endregion

    #region When the plan cannot be read

    [Fact]
    public async Task AnUnidentifiableAccountIsRefusedWithItsOwnMessage()
    {
        var erabliereApi = new FakeErabliereApi { PlanStatusCode = HttpStatusCode.NotFound, PlanBody = "\"Utilisateur non trouvé.\"" };

        await using var app = await StartServerAsync(erabliereApi, GatingConfiguration(enabled: true));
        using var httpClient = CreateHttpClient(app);

        using var response = await ListToolsAsync(httpClient);

        var error = (await ReadJsonAsync(response)).GetProperty("error");

        error.GetProperty("code").GetInt32().ShouldBe(JsonRpcErrorWriter.PlanUnavailableErrorCode);
        error.GetProperty("message").GetString()!.ShouldContain("could not tell which account");
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task AKeyRefusedByTheApiIsRefusedHereToo(HttpStatusCode statusCode)
    {
        var erabliereApi = new FakeErabliereApi { PlanStatusCode = statusCode, PlanBody = "" };

        await using var app = await StartServerAsync(erabliereApi, GatingConfiguration(enabled: true));
        using var httpClient = CreateHttpClient(app);

        using var response = await ListToolsAsync(httpClient);

        var error = (await ReadJsonAsync(response)).GetProperty("error");

        error.GetProperty("code").GetInt32().ShouldBe(JsonRpcErrorWriter.PlanUnavailableErrorCode);
        error.GetProperty("message").GetString()!.ShouldContain("refused this api key");
    }

    [Fact]
    public async Task AnUnreachableApiFailsClosed()
    {
        // Letting an unknown plan through would make the gate a suggestion: a client
        // pointed at an unreachable API would gain the access it is denied.
        var erabliereApi = new FakeErabliereApi { Unreachable = true };

        await using var app = await StartServerAsync(erabliereApi, GatingConfiguration(enabled: true));
        using var httpClient = CreateHttpClient(app);

        using var response = await ListToolsAsync(httpClient);

        var error = (await ReadJsonAsync(response)).GetProperty("error");

        error.GetProperty("code").GetInt32().ShouldBe(JsonRpcErrorWriter.PlanUnavailableErrorCode);
        error.GetProperty("message").GetString()!.ShouldContain("could not be reached");
    }

    #endregion

    #region The cache

    [Fact]
    public async Task TheResolvedPlanIsCachedAcrossRequests()
    {
        // An MCP session sends many requests and a plan changes a few times a year;
        // one round trip per tool call would be paid for nothing.
        var erabliereApi = new FakeErabliereApi();

        await using var app = await StartServerAsync(erabliereApi, GatingConfiguration(enabled: true, cacheDuration: "00:05:00"));
        using var httpClient = CreateHttpClient(app);

        (await ListToolsAsync(httpClient)).Dispose();
        (await ListToolsAsync(httpClient)).Dispose();
        (await ListToolsAsync(httpClient)).Dispose();

        erabliereApi.PlanRequestCount.ShouldBe(1);
    }

    [Fact]
    public async Task TwoApiKeysNeverShareACachedPlan()
    {
        // A regression here would hand the plan of a subscriber to anyone else.
        var erabliereApi = new FakeErabliereApi();

        await using var app = await StartServerAsync(erabliereApi, GatingConfiguration(enabled: true, cacheDuration: "00:05:00"));

        using var subscriber = CreateHttpClient(app, "the-subscriber-key");

        (await ListToolsAsync(subscriber)).Dispose();

        erabliereApi.PlanBody = FakeErabliereApi.PlanJson("gratuit", abonnementActif: false);

        using var freeloader = CreateHttpClient(app, "another-key");

        using var response = await ListToolsAsync(freeloader);

        (await ReadJsonAsync(response)).GetProperty("error")
                                       .GetProperty("code")
                                       .GetInt32()
                                       .ShouldBe(JsonRpcErrorWriter.PlanRequiredErrorCode);

        erabliereApi.PlanRequestCount.ShouldBe(2);
    }

    [Fact]
    public async Task ACacheDurationOfZeroAsksEveryTime()
    {
        var erabliereApi = new FakeErabliereApi();

        await using var app = await StartServerAsync(erabliereApi, GatingConfiguration(enabled: true, cacheDuration: "00:00:00"));
        using var httpClient = CreateHttpClient(app);

        (await ListToolsAsync(httpClient)).Dispose();
        (await ListToolsAsync(httpClient)).Dispose();

        erabliereApi.PlanRequestCount.ShouldBe(2);
    }

    #endregion

    #region stdio stays ungated

    [Fact]
    public async Task TheStdioCompositionRunsTheToolsWhateverThePlan()
    {
        // The gate is a piece of the HTTP pipeline, and a stdio server has no HTTP
        // pipeline at all: it is started by the user on their own machine with their
        // own key and answers no one else. This pins down that nothing of the gate
        // leaked into the tools themselves, which are shared by both transports.
        var erabliereApi = new FakeErabliereApi { PlanBody = FakeErabliereApi.PlanJson("gratuit", abonnementActif: false) };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(GatingConfiguration(enabled: true))
            {
                [ErabliereApiMcpOptions.ApiKeyEnvironmentVariable] = TestApiKey
            })
            .Build();

        var services = new ServiceCollection();

        services.AddLogging();
        services.AddErabliereApiProxy(configuration, McpTransportMode.Stdio);
        services.AddHttpClient(ErabliereApiMcpOptions.HttpClientName)
                .ConfigurePrimaryHttpMessageHandler(() => erabliereApi);

        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var proxy = scope.ServiceProvider.GetRequiredService<IErabliereAPIProxy>();

        var response = await ErabliereTools.ListErablieresAsync(proxy, cancellationToken: CancellationToken.None);

        response.Data.ShouldBeEmpty();
        erabliereApi.PlanRequestCount.ShouldBe(0);
    }

    #endregion
}
