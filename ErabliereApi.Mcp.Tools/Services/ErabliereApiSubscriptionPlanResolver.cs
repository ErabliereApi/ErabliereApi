using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ErabliereApi.Mcp.Configuration;
using ErabliereApi.Mcp.Http;
using ErabliereApi.Mcp.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ErabliereApi.Mcp.Services;

/// <summary>
/// Reads the current plan from ErabliereAPI, which owns the subscriptions.
/// </summary>
/// <remarks>
/// No plan logic is reimplemented here. <c>GET /api/Abonnements/Courant</c> answers
/// from the very <c>IAbonnementService</c> the API's own
/// <c>ValiderAbonnementAttribute</c> uses, so the MCP server and the API can never
/// disagree on who is on what plan.
/// </remarks>
public class ErabliereApiSubscriptionPlanResolver : ISubscriptionPlanResolver
{
    /// <summary>
    /// Path of the endpoint returning the current plan.
    /// </summary>
    public const string PlanPath = "/api/Abonnements/Courant";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IApiKeyAccessor _apiKeyAccessor;
    private readonly IMemoryCache _cache;
    private readonly IOptions<ErabliereApiMcpOptions> _apiOptions;
    private readonly IOptions<McpPlanGatingOptions> _gatingOptions;
    private readonly ILogger<ErabliereApiSubscriptionPlanResolver> _logger;

    /// <summary>
    /// Creates the resolver.
    /// </summary>
    public ErabliereApiSubscriptionPlanResolver(
        IHttpClientFactory httpClientFactory,
        IApiKeyAccessor apiKeyAccessor,
        IMemoryCache cache,
        IOptions<ErabliereApiMcpOptions> apiOptions,
        IOptions<McpPlanGatingOptions> gatingOptions,
        ILogger<ErabliereApiSubscriptionPlanResolver> logger)
    {
        _httpClientFactory = httpClientFactory;
        _apiKeyAccessor = apiKeyAccessor;
        _cache = cache;
        _apiOptions = apiOptions;
        _gatingOptions = gatingOptions;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SubscriptionPlan> GetCurrentPlanAsync(CancellationToken cancellationToken)
    {
        var apiKey = _apiKeyAccessor.GetApiKey();

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new SubscriptionPlanUnavailableException(
                $"No ErabliereAPI api key was sent, so no subscription plan could be read. Send the {ApiKeyHandler.XApiKeyHeader} header.");
        }

        var cacheDuration = _gatingOptions.Value.CacheDuration;
        var cacheKey = CacheKey(apiKey);

        if (cacheDuration > TimeSpan.Zero && _cache.TryGetValue<SubscriptionPlan>(cacheKey, out var cached) && cached != null)
        {
            return cached;
        }

        var plan = await FetchPlanAsync(cancellationToken);

        if (cacheDuration > TimeSpan.Zero)
        {
            // Absolute, not sliding: a sliding window on a busy session would never
            // expire, and a cancelled subscription would keep its access forever.
            _cache.Set(cacheKey, plan, cacheDuration);
        }

        return plan;
    }

    private async Task<SubscriptionPlan> FetchPlanAsync(CancellationToken cancellationToken)
    {
        // The named client is the one the tools use: its handlers add the caller's
        // api key and retry the transient failures, so this call is authenticated as
        // whoever is knocking, exactly like the tool calls that follow it.
        var httpClient = _httpClientFactory.CreateClient(ErabliereApiMcpOptions.HttpClientName);

        var url = $"{_apiOptions.Value.BaseUrl.TrimEnd('/')}{PlanPath}";

        HttpResponseMessage response;

        try
        {
            response = await httpClient.GetAsync(url, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "ErabliereAPI could not be reached to resolve the subscription plan.");

            throw new SubscriptionPlanUnavailableException(
                "ErabliereAPI could not be reached to check the subscription plan of this api key. Try again in a moment.", exception);
        }

        using (response)
        {
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                // ErabliereAPI is the single authority on api keys; it just refused
                // this one. Saying so beats a generic "plan unavailable".
                throw new SubscriptionPlanUnavailableException(
                    "ErabliereAPI refused this api key. Check that it is still active and that it is allowed to call GET /api/Abonnements.");
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new SubscriptionPlanUnavailableException(
                    "ErabliereAPI could not tell which account this api key belongs to, so no subscription plan could be read. " +
                    "This is what a server with the Stripe integration disabled answers, in which case the plan gate should be turned off.");
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("ErabliereAPI answered {StatusCode} on {Path} while resolving the subscription plan.",
                    (int)response.StatusCode, PlanPath);

                throw new SubscriptionPlanUnavailableException(
                    $"ErabliereAPI answered {(int)response.StatusCode} when asked for the subscription plan of this api key.");
            }

            SubscriptionPlan? plan;

            try
            {
                plan = await response.Content.ReadFromJsonAsync<SubscriptionPlan>(JsonOptions, cancellationToken);
            }
            catch (JsonException exception)
            {
                throw new SubscriptionPlanUnavailableException(
                    $"The answer of ErabliereAPI on {PlanPath} could not be read. The API may be older than the subscription feature.", exception);
            }

            if (plan == null || string.IsNullOrWhiteSpace(plan.Plan))
            {
                throw new SubscriptionPlanUnavailableException(
                    $"ErabliereAPI returned no plan on {PlanPath}. The API may be older than the subscription feature.");
            }

            return plan;
        }
    }

    /// <summary>
    /// Cache key of an api key. The key itself never becomes one: a cache entry can
    /// end up in a dump or a log line, and a credential should survive neither.
    /// </summary>
    private static string CacheKey(string apiKey)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(apiKey));

        return $"plan:{Convert.ToHexString(hash)}";
    }
}
