using ErabliereApi.Mcp.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace ErabliereApi.Mcp.Health;

/// <summary>
/// Readiness probe: this server is only ever a front for ErabliereAPI, so it is
/// ready when ErabliereAPI answers.
/// </summary>
/// <remarks>
/// The probe calls the anonymous <c>/health</c> endpoint of ErabliereAPI through
/// a dedicated <see cref="HttpClient"/> that carries neither the api key handler
/// nor the retry handler: an orchestrator wants the current state fast, not a
/// state retried for six seconds.
/// </remarks>
public class ErabliereApiHealthCheck : IHealthCheck
{
    /// <summary>
    /// Name of the named <see cref="HttpClient"/> used by the probe.
    /// </summary>
    public const string HttpClientName = "ErabliereAPI-Health";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<ErabliereApiMcpOptions> _options;

    /// <summary>
    /// Creates the health check.
    /// </summary>
    public ErabliereApiHealthCheck(IHttpClientFactory httpClientFactory, IOptions<ErabliereApiMcpOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var baseUrl = _options.Value.BaseUrl;

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
        {
            return HealthCheckResult.Unhealthy(
                $"The environment variable {ErabliereApiMcpOptions.BaseUrlEnvironmentVariable} does not hold an absolute url.");
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient(HttpClientName);

            using var response = await httpClient.GetAsync(new Uri(uri, "health"), cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy($"ErabliereAPI answered {(int)response.StatusCode} at {uri}.")
                : HealthCheckResult.Unhealthy($"ErabliereAPI answered {(int)response.StatusCode} at {uri}.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException ||
                                          !cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy($"ErabliereAPI is unreachable at {uri}.", exception);
        }
    }
}
