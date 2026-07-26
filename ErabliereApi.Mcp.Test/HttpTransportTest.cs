using System.Net;
using ErabliereApi.Mcp.Configuration;
using ErabliereApi.Mcp.Hosting;
using ErabliereApi.Mcp.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;
using Shouldly;

namespace ErabliereApi.Mcp.Test;

/// <summary>
/// Boots the HTTP transport in memory and drives it with a real MCP client, so
/// the initialize / tools/list round trip is exercised end to end rather than
/// asserted on the registration code.
/// </summary>
/// <remarks>
/// The test server runs the very composition of <see cref="HttpServerRunner"/>;
/// nothing about the pipeline is re-declared here.
/// </remarks>
public class HttpTransportTest : IAsyncLifetime
{
    private const string TestApiKey = "an-api-key";

    private WebApplication _app = null!;
    private readonly RecordingErabliereApi _erabliereApi = new();

    /// <summary>
    /// Stands in for ErabliereAPI and records the api key each call carried.
    /// </summary>
    private sealed class RecordingErabliereApi : HttpMessageHandler
    {
        public List<string?> ReceivedApiKeys { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            lock (ReceivedApiKeys)
            {
                ReceivedApiKeys.Add(request.Headers.TryGetValues(ApiKeyHandler.XApiKeyHeader, out var values)
                    ? values.FirstOrDefault()
                    : null);
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]", System.Text.Encoding.UTF8, "application/json")
            });
        }
    }

    /// <summary>
    /// Starts the MCP server on a test server.
    /// </summary>
    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();

        builder.WebHost.UseTestServer();

        // ErabliereAPI is never called by initialize or tools/list, so the url only
        // has to be a valid absolute one for the configuration check to pass.
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [ErabliereApiMcpOptions.BaseUrlEnvironmentVariable] = "https://erabliereapi.test"
        });

        HttpServerRunner.ConfigureServices(builder);

        // Substitute ErabliereAPI itself, leaving the api key and retry handlers of
        // the real registration in place: what is under test is the key travelling
        // from the incoming MCP request to the outgoing API call.
        builder.Services.AddHttpClient(ErabliereApiMcpOptions.HttpClientName)
                        .ConfigurePrimaryHttpMessageHandler(() => _erabliereApi);

        _app = builder.Build();

        HttpServerRunner.MapEndpoints(_app);

        await _app.StartAsync();
    }

    /// <summary>
    /// Stops the test server.
    /// </summary>
    public async Task DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    private HttpClient CreateHttpClient(string? apiKey)
    {
        var httpClient = _app.GetTestClient();

        if (apiKey is not null)
        {
            httpClient.DefaultRequestHeaders.Add(ApiKeyHandler.XApiKeyHeader, apiKey);
        }

        return httpClient;
    }

    private async Task<McpClient> CreateMcpClientAsync(string apiKey = TestApiKey)
    {
        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri($"http://localhost{HttpServerRunner.McpPath}"),
                // Explicit: the server runs stateless, which disables the legacy
                // "/sse" endpoint the auto-detecting mode would fall back to.
                TransportMode = HttpTransportMode.StreamableHttp
            },
            CreateHttpClient(apiKey),
            loggerFactory: null,
            ownsHttpClient: true);

        return await McpClient.CreateAsync(transport);
    }

    [Fact]
    public async Task TheServerAnswersInitializeAndListsItsTools()
    {
        await using var client = await CreateMcpClientAsync();

        var tools = await client.ListToolsAsync();

        // The same curated set as the stdio transport: one assembly, two transports.
        tools.Select(tool => tool.Name)
             .OrderBy(name => name, StringComparer.Ordinal)
             .ShouldBe([
                 "get_alertes",
                 "get_barils",
                 "get_dompeux",
                 "get_donnees_capteur",
                 "get_erabliere",
                 "get_horaire",
                 "get_my_plan",
                 "get_notes",
                 "get_rapport",
                 "list_capteurs",
                 "list_erablieres",
                 "list_rapports"
             ]);
    }

    [Fact]
    public async Task TheServerAnnouncesItselfOnInitialize()
    {
        await using var client = await CreateMcpClientAsync();

        client.ServerInfo.Name.ShouldNotBeNullOrWhiteSpace();
        client.ServerCapabilities.Tools.ShouldNotBeNull();
    }

    [Fact]
    public async Task TheToolDescriptionsSurviveTheRoundTrip()
    {
        // The descriptions are the contract the model reads; a transport that
        // dropped them would leave a technically working but unusable server.
        await using var client = await CreateMcpClientAsync();

        var tools = await client.ListToolsAsync();

        tools.ShouldAllBe(tool => tool.Description != null && tool.Description.Length > 40);
    }

    [Fact]
    public async Task ATooCallForwardsTheCallersApiKeyToErabliereApi()
    {
        // The point of the HTTP transport: the server holds no key of its own, it
        // relays the one of whoever is calling.
        await using var client = await CreateMcpClientAsync("the-caller-key");

        await client.CallToolAsync("list_erablieres");

        _erabliereApi.ReceivedApiKeys.ShouldHaveSingleItem().ShouldBe("the-caller-key");
    }

    [Fact]
    public async Task TwoCallersNeverBorrowEachOthersApiKey()
    {
        // A regression here would be a cross-tenant data leak, not a bug: the
        // second caller would read the maple groves of the first one.
        await using var first = await CreateMcpClientAsync("first-caller-key");
        await using var second = await CreateMcpClientAsync("second-caller-key");

        await first.CallToolAsync("list_erablieres");
        await second.CallToolAsync("list_erablieres");
        await first.CallToolAsync("list_erablieres");

        _erabliereApi.ReceivedApiKeys.ShouldBe(["first-caller-key", "second-caller-key", "first-caller-key"]);
    }

    [Fact]
    public async Task WithoutTheApiKeyHeader_TheMcpEndpointAnswers401()
    {
        // The gate is in front of the endpoint, not inside the tools: the tool
        // catalog names the features of an account and is not public.
        using var httpClient = CreateHttpClient(apiKey: null);

        using var response = await httpClient.PostAsync(
            HttpServerRunner.McpPath,
            new StringContent("""{"jsonrpc":"2.0","id":1,"method":"tools/list"}""", System.Text.Encoding.UTF8, "application/json"));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Headers.WwwAuthenticate.ToString().ShouldContain(ApiKeyHandler.XApiKeyHeader);
    }

    [Fact]
    public async Task WithAnEmptyApiKeyHeader_TheMcpEndpointAnswers401()
    {
        using var httpClient = CreateHttpClient("   ");

        using var response = await httpClient.PostAsync(
            HttpServerRunner.McpPath,
            new StringContent("""{"jsonrpc":"2.0","id":1,"method":"tools/list"}""", System.Text.Encoding.UTF8, "application/json"));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TheLivenessProbeIsHealthyAndAnonymous()
    {
        using var httpClient = CreateHttpClient(apiKey: null);

        using var response = await httpClient.GetAsync(HttpServerRunner.LivePath);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldBe("Healthy");
    }

    [Fact]
    public async Task TheReadinessProbeIsAnonymousAndReportsTheUnreachableApi()
    {
        // https://erabliereapi.test does not resolve, so the readiness probe must
        // say the server is not ready rather than answer 200 on a broken backend.
        using var httpClient = CreateHttpClient(apiKey: null);

        using var response = await httpClient.GetAsync(HttpServerRunner.HealthPath);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }
}
