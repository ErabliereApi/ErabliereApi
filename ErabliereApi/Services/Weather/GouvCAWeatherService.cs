using System.Text.Json;
using ErabliereApi.Extensions;
using ErabliereApi.Services.AccuWeatherModels;
using ErabliereApi.Services.GouvCAModels;
using Microsoft.Extensions.Caching.Distributed;

namespace ErabliereApi.Services;

/// <summary>
/// Service pour interagir avec les prévisions météo depuis l'API du gouvernement du canada
/// </summary>
public class GouvCAWeatherService : IWeaterService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDistributedCache _cache;
    private readonly ILogger<GouvCAWeatherService> _logger;

    /// <summary>
    /// Constructeur
    /// </summary>
    public GouvCAWeatherService(
        IDistributedCache memoryCache,
        ILogger<GouvCAWeatherService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        _cache = memoryCache;
        _logger = logger;
    }

    /// <summary>
    /// Obtenir le code de localisation à partir d'un code postal
    /// </summary>
    public async Task<(int, string)> GetLocationCodeAsync(GetLocationCodeArgs arg, CancellationToken cancellationToken)
    {
        var cacheKey = $"WeatherService.PostalCode.{arg.PostalCode}";
        var locationCode = await _cache.GetStringAsync(cacheKey);
        if (!string.IsNullOrWhiteSpace(locationCode))
        {
            return (200, locationCode);
        }

        string responseBodyString = string.Empty;

        try
        {
            var lat = arg.Latitude.ToString("F3").Replace(',', '.');
            var longi = arg.Longitude.ToString("F3").Replace(',', '.');

            var locationKey = $"{lat},{longi}";

            await _cache.SetStringAsync(cacheKey, locationKey);

            return (200, locationKey);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Error retrieving location code: {Message} {Response}", ex.Message, responseBodyString);
            return (500, responseBodyString);
        }
    }

    /// <summary>
    /// Obtenir les prévisions météo à partir d'un code de localisation
    /// </summary>
    public async ValueTask<WeatherForecastResponse?> GetWeatherForecastAsync(string location, string lang, CancellationToken cancellationToken)
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
            var culture = lang;
            lang = lang == "fr-ca" ? "fr" : lang;

            string url = $"/api/app/v3/{lang}/Location/{location}";

            var _httpClient = _httpClientFactory.CreateClient("WeatherStationGouvCA");

            HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var rawData = await DeserializeWeatherCAResponse(response, cancellationToken);

            var responseBody = rawData.Single().ToWeatherForecastResponse(culture);

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

    private async Task<GouvCAWeatherStationResponse[]> DeserializeWeatherCAResponse(HttpResponseMessage response, CancellationToken token)
    {
        var str = await response.Content.ReadAsStringAsync(token);

        try
        {
            var rawData = JsonSerializer.Deserialize<GouvCAWeatherStationResponse[]>(str)
                    ?? throw new InvalidOperationException($"weather forecast is null ater deserialize to GouvCAWeatherStationResponse {str}");

            return rawData;
        }
        catch (JsonException e)
        {
            throw new InvalidDataException($"Cannot deserialize json {str}", e);
        }
    }

    /// <summary>
    /// Obtenir les prévisions météo horaires à partir d'un code de localisation
    /// </summary>
    public async ValueTask<HourlyWeatherForecastResponse[]?> GetHourlyForecastAsync(string location, string lang, CancellationToken cancellationToken)
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
            var culture = lang;
            lang = lang == "fr-ca" ? "fr" : lang;

            string url = $"/api/app/v3/{lang}/Location/{location}";

            var _httpClient = _httpClientFactory.CreateClient("WeatherStationGouvCA");

            HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var rawData = await DeserializeWeatherCAResponse(response, cancellationToken);

            var responseBody = rawData.Single().ToHourlyWeatherForecastResponse(culture);

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
