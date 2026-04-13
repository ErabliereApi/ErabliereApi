using ErabliereApi.Donnees.Interfaces;
using ErabliereApi.Services.AccuWeatherModels;

namespace ErabliereApi.Services;

/// <summary>
/// Argument de GetLocationCode
/// </summary>
public class GetLocationCodeArgs : ILocalizable
{
    /// <summary>
    /// Code postal
    /// </summary>
    public string PostalCode { get; set; } = "";

    /// <summary>
    /// Latitude
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// Longitude
    /// </summary>
    public double Longitude { get; set; }
}

/// <summary>
/// Interface de service pour les données météorologiques
/// </summary>
public interface IWeaterService
{
    /// <summary>
    /// Obtenir le code de localisation à partir d'un code postal
    /// </summary>
    Task<(int, string)> GetLocationCodeAsync(GetLocationCodeArgs arg, CancellationToken cancellationToken);

    /// <summary>
    /// Obtenir les prédiction météorologiques pour une localisation pour 5 jours
    /// </summary>
    ValueTask<WeatherForecastResponse?> GetWeatherForecastAsync(string location, string lang, CancellationToken cancellationToken);

    /// <summary>
    /// Obtenir les prédiction météorologiques horaire pour une localisation pour 12 heures
    /// </summary>
    ValueTask<HourlyWeatherForecastResponse[]?> GetHourlyForecastAsync(string location, string lang, CancellationToken cancellationToken);
}
