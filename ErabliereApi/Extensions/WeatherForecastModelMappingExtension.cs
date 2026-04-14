using System.Text.Json;
using ErabliereApi.Services.AccuWeatherModels;
using ErabliereApi.Services.GouvCAModels;

namespace ErabliereApi.Extensions;

/// <summary>
/// Classe des méthodes d'extension pour le mapping des modèle météo
/// </summary>
public static class WeatherForecastModelMappingExtension
{
    /// <summary>
    /// Prendre la réponse des prédiction météo du service de météo du Canada et les convertir en modèle WeatherForecastResponse.
    /// </summary>
    /// <param name="gouvCAWeatherStationResponse"></param>
    /// <returns></returns>
    public static string ToWeatherForecastResponse(this GouvCAWeatherStationResponse gouvCAWeatherStationResponse)
    {
        var forecast = new WeatherForecastResponse
        {
             Headline = new Headline
             {
                 
             },
             DailyForecasts = gouvCAWeatherStationResponse.dailyFcst?.daily?.Select(d =>
                 new Dailyforecast
                 {
                     
                 }
             ).ToArray() ?? []
        };

        return JsonSerializer.Serialize(forecast);
    }

    /// <summary>
    /// Prendre la réponse des prédictions météo du service de météo du Canada et les conertir en modèle HourlyWeatherForecastResponse.
    /// </summary>
    /// <param name="gouvCAWeatherStationResponse"></param>
    /// <returns></returns>
    public static string ToHourlyWeatherForecastResponse(this GouvCAWeatherStationResponse gouvCAWeatherStationResponse)
    {
       var forecast = gouvCAWeatherStationResponse.hourlyFcst?.hourly?.Select(h => 
            new HourlyWeatherForecastResponse
            {
                
            }
        ).ToArray() ?? [];

       return JsonSerializer.Serialize(forecast);
    }
}