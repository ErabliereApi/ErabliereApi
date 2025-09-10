
using ErabliereApi.Services.AccuWeatherModels;
using ErabliereApi.Services.MeteoMaticModels;

namespace ErabliereApi.Services;

/// <summary>
/// IWeatherService implementation for MeteoMatic API.
/// </summary>
public class MeteoMaticWeaterService : IWeaterService
{
    /// <inheritdoc />
    public async Task<(int, string)> GetLocationCodeAsync(string postalCode, CancellationToken cancellationToken)
    {
        using var http = new HttpClient();

        var response = await http.GetFromJsonAsync<MeteoMaticLocationResponse>($"https://geocoder.meteomatics.com/api/v1/geocoder/direct/?location={postalCode}&language=en&limit=8", cancellationToken);

        var geo = response?.result?.FirstOrDefault()?.geometry;

        if (geo == null)
        {
            return (404 , "");
        }

        return (200, $"{geo.lat},{geo.lng}");
    }

    /// <inheritdoc />
    public ValueTask<WeatherForecastResponse?> GetWeatherForecastAsync(string location, string lang, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ValueTask<HourlyWeatherForecastResponse[]?> GetHourlyForecastAsync(string location, string lang, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
