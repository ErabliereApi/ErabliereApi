using System.Text.Json;
using ErabliereApi.Services.AccuWeatherModels;
using Microsoft.Extensions.Caching.Distributed;

namespace ErabliereApi.Services;

/// <summary>
/// Service pour interagir avec les prévisions météo
/// </summary>
public class WeatherService : IWeaterService
{
    private readonly HttpClient _httpClient;
    private readonly IDistributedCache _cache;
    private readonly ILogger<WeatherService> _logger;
    private readonly string? AccuWeatherApiKey;
    private readonly string? AccuWeatherBaseUrl;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Constructeur
    /// </summary>
    public WeatherService(
        IDistributedCache memoryCache,
        ILogger<WeatherService> logger,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = new HttpClient();
        _cache = memoryCache;
        _logger = logger;
        AccuWeatherApiKey = configuration["AccuWeatherApiKey"];
        AccuWeatherBaseUrl = configuration["AccuWeatherBaseUrl"];
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Obtenir le code de localisation à partir d'un code postal
    /// </summary>
    public async ValueTask<string> GetLocationCodeAsync(string postalCode)
    {
        var cacheKey = $"WeatherService.PostalCode.{postalCode}";
        var locationCode = await _cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrWhiteSpace(locationCode))
        {
            return locationCode;
        }

        string responseBodyString = string.Empty;

        try
        {
            string url = $"{AccuWeatherBaseUrl}/locations/v1/postalcodes/search?apikey={AccuWeatherApiKey}&q={postalCode}";

            HttpResponseMessage response = await _httpClient.GetAsync(url);

            responseBodyString = await response.Content.ReadAsStringAsync();

            response.EnsureSuccessStatusCode();

            var responseBody = JsonSerializer.Deserialize<List<LocationResponse>>(responseBodyString);
            
            if (responseBody == null || responseBody.Count == 0)
            {
                return "";
            }

            var locationKey = responseBody[0].Key ?? "";

            await _cache.SetStringAsync(cacheKey, locationKey);

            return locationKey;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Error retrieving location code: {Message} {Response}", ex.Message, responseBodyString);
            return "";
        }
    }

    /// <summary>
    /// Obtenir les prévisions météo à partir d'un code de localisation
    /// </summary>
    public async ValueTask<WeatherForecastResponse?> GetWeatherForecastAsync(string location, string lang)
    {
        var cacheKey = $"WeatherService.GetWeatherForecast.{location}";
        var cacheValue = await _cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrWhiteSpace(cacheValue))
        {
            var res = JsonSerializer.Deserialize<WeatherForecastResponse>(cacheValue);
            return res;
        }

        try
        {
            string url = $"{AccuWeatherBaseUrl}/forecasts/v1/daily/5day/{location}?apikey={AccuWeatherApiKey}&language={lang}";

            HttpResponseMessage response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            if (response.Headers.TryGetValues("RateLimit-Remaining", out var vals))
            {
                var httpContext = _httpContextAccessor.HttpContext;

                httpContext?.Response.Headers.Append("X-ErabliereApi-AccuWeather-RateLimit-Remaining", vals.FirstOrDefault());
            }

            var responseBody = await response.Content.ReadAsStringAsync();

            await _cache.SetStringAsync(cacheKey , responseBody, new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.Date + TimeSpan.FromDays(1),
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6)
            });

            var res = JsonSerializer.Deserialize<WeatherForecastResponse>(responseBody);
            return res;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Error retrieving weather forecast: {Message}", ex.Message);
            return new WeatherForecastResponse();
        }
    }

    /// <summary>
    /// Obtenir les prévisions météo horaires à partir d'un code de localisation
    /// </summary>
    public async ValueTask<HourlyWeatherForecastResponse[]?> GetHoulyForecastAsync(string location, string lang)
    {
        var cacheKey = $"WeatherService.GetHoulyForecastAsync.{location}";
        var cacheValue = await _cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrWhiteSpace(cacheValue))
        {
            var res = JsonSerializer.Deserialize<HourlyWeatherForecastResponse[]>(cacheValue);
            return res;
        }

        try
        {
            string url = 
$"{AccuWeatherBaseUrl}/forecasts/v1/hourly/12hour/{location}?apikey={AccuWeatherApiKey}&language={lang}";

            HttpResponseMessage response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            if (response.Headers.TryGetValues("RateLimit-Remaining", out var vals))
            {
                var httpContext = _httpContextAccessor.HttpContext;

                _logger.LogInformation("RateLimit-Remaining: {0}", vals.FirstOrDefault());

                httpContext?.Response.Headers.Append("X-ErabliereApi-AccuWeather-RateLimit-Remaining", vals.FirstOrDefault());
            }

            var responseBody = await response.Content.ReadAsStringAsync();

            await _cache.SetStringAsync(cacheKey , responseBody, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });

            var res = JsonSerializer.Deserialize<HourlyWeatherForecastResponse[]>(responseBody);
            return res;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Error retrieving hourly weather forecast: {Message}", ex.Message);
            return [];
        }
    }
}
