using ErabliereApi.Services.AccuWeatherModels;

namespace ErabliereApi.Services;

/// <summary>
/// Interface de service pour les données météorologiques
/// </summary>
public interface IWeaterService
{
    /// <summary>
    /// Obtenir le code de localisation à partir d'un code postal
    /// </summary>
    Task<(int, string)> GetLocationCodeAsync(string postalCode, CancellationToken cancellationToken);

    /// <summary>
    /// Obtenir les prédiction météorologiques pour une localisation pour 5 jours
    /// </summary>
    ValueTask<WeatherForecastResponse?> GetWeatherForecastAsync(string location, string lang, CancellationToken cancellationToken);

    /// <summary>
    /// Obtenir les prédiction météorologiques horaire pour une localisation pour 12 heures
    /// </summary>
    ValueTask<HourlyWeatherForecastResponse[]?> GetHourlyForecastAsync(string location, string lang, CancellationToken cancellationToken);
}
