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
    ValueTask<string> GetLocationCodeAsync(string postalCode);

    /// <summary>
    /// Obtenir les prédiction météorologiques pour une localisation pour 5 jours
    /// </summary>
    ValueTask<WeatherForecastResponse?> GetWeatherForecastAsync(string location, string lang);

    /// <summary>
    /// Obtenir les prédiction météorologiques horaire pour une localisation pour 12 heures
    /// </summary>
    ValueTask<HourlyWeatherForecastResponse[]?> GetHoulyForecastAsync(string location, string lang);
}
