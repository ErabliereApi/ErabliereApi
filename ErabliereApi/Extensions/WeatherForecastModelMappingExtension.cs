using ErabliereApi.Services.AccuWeatherModels;
using ErabliereApi.Services.GouvCAModels;
using System.Globalization;
using System.Text.Json;

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
    /// <param name="cultureStr">Culture pour le parsing des dates</param>
    /// <param name="coord">Les coordonnées</param>
    /// <returns></returns>
    public static string ToWeatherForecastResponse(this GouvCAWeatherStationResponse gouvCAWeatherStationResponse, string cultureStr, string coord)
    {
        var culture = new CultureInfo(cultureStr);
        bool hasDecember = false;

        var forcastGroup = gouvCAWeatherStationResponse.dailyFcst?.daily?.GroupBy(d => d.date);

        if (forcastGroup == null)
        {
            throw new InvalidOperationException("forecastGroup can't be null after grouping");
        }

        var firstGroup = true;
        var i = 1;
        var count = forcastGroup.Count();

        var forecast = new WeatherForecastResponse
        {
            Headline = new Headline
            {
                EffectiveDate = gouvCAWeatherStationResponse.observation?.timeStamp,
                EffectiveEpochDate = gouvCAWeatherStationResponse.lastUpdated,
                Category = gouvCAWeatherStationResponse.observation?.condition,
                EndDate = gouvCAWeatherStationResponse.observation?.timeStamp,
                EndEpochDate = gouvCAWeatherStationResponse.lastUpdated + 200,
                Severity = 3,
                Text = gouvCAWeatherStationResponse.dailyFcst?.daily?.FirstOrDefault()?.text,
                Link = "https://meteo.gc.ca/fr/location/index.html?coords=" + coord,
                MobileLink = "https://meteo.gc.ca/fr/location/index.html?coords=" + coord,
            },
            DailyForecasts = forcastGroup?.Select(g =>
            {
                var key = g.Key;
                Daily? day = firstGroup && g.Count() == 1 ? null : g.First();
                Daily? night = i == count && g.Count() == 1 ? g.First() : g.Last();
                i++;
                DateTime? datedf = null;
                DateTimeOffset? datedfoffset = null;
                if (key != null)
                {
                    key = key.Replace(",", ".,");
                    if (!key.EndsWith("mai"))
                    {
                        key += ".";
                    }
                    datedf = DateTime.ParseExact(key, "ddd, d MMM", culture, DateTimeStyles.AssumeLocal);

                    hasDecember |= datedf.Value.Month == 12;
                    if (hasDecember && datedf.Value.Month == 1)
                    {
                        datedf = new DateTime(
                            datedf.Value.Year + 1,
                            datedf.Value.Month,
                            datedf.Value.Day,
                            datedf.Value.Hour,
                            datedf.Value.Minute,
                            datedf.Value.Second);
                    }
                }

                datedfoffset = datedf;

                var df = new Dailyforecast
                {
                    Date = datedf,
                    EpochDate = datedfoffset?.ToUnixTimeSeconds() ?? 0,
                    Day = new Day
                    {
                        HasPrecipitation = day != null && day.precip != "",
                        Icon = day?.iconCode != null ? int.Parse(day.iconCode) : 0,
                        IconPhrase = day?.summary,
                        PrecipitationIntensity = day?.precip,
                        PrecipitationType = day?.text
                    },
                    Night = new Night
                    {
                        HasPrecipitation = night != null && night.precip != "",
                        Icon = night?.iconCode != null ? int.Parse(night.iconCode) : 0,
                        IconPhrase = night?.summary,
                        PrecipitationIntensity = day?.precip,
                        PrecipitationType = day?.text
                    },
                    Sources = null,
                    Link = "https://meteo.gc.ca/fr/location/index.html?coords=" + coord,
                    MobileLink = "https://meteo.gc.ca/fr/location/index.html?coords=" + coord,
                    Temperature = new Services.AccuWeatherModels.Temperature
                    {
                        Maximum = new Maximum { Value = g.Max(ge => ge.temperature?.imperial.AsFloat()) },
                        Minimum = new Minimum { Value = g.Min(ge => ge.temperature?.imperial.AsFloat()) }
                    }
                };

                return df;
            }).ToArray() ?? []
        };

        return JsonSerializer.Serialize(forecast);
    }

    /// <summary>
    /// Prendre la réponse des prédictions météo du service de météo du Canada et les conertir en modèle HourlyWeatherForecastResponse.
    /// </summary>
    /// <param name="gouvCAWeatherStationResponse"></param>
    /// <param name="cultureStr">Culture pour le parsing des dates</param>
    /// <returns></returns>
    public static string ToHourlyWeatherForecastResponse(this GouvCAWeatherStationResponse gouvCAWeatherStationResponse, string cultureStr)
    {
        var culture = new CultureInfo(cultureStr);

        var forecast = gouvCAWeatherStationResponse.hourlyFcst?.hourly?.Select(h =>
             new HourlyWeatherForecastResponse
             {
                 DateTime = DateTimeOffset.FromUnixTimeSeconds(h.epochTime),
                 EpochDateTime = h.epochTime,
                 Temperature = h.temperature?.metric != null ? new HourlyForecastTemperature
                 {
                     Value = double.Parse(h.temperature.metric),
                     Unit = "C",
                     UnitType = 17
                 } : null,
                 HasPrecipitation = h.precip != "",
                 WeatherIcon = h.iconCode != null ? int.Parse(h.iconCode) : 0,
                 IconPhrase = h.condition,
                 PrecipitationType = h.condition,
                 PrecipitationIntensity = h.precip != "" ? h.precip : null,
                 Link = null,
                 MobileLink = null
             }
         ).ToArray() ?? [];

        return JsonSerializer.Serialize(forecast);
    }

    /// <summary>
    /// Convertie un entier nullable en float
    /// Si null, return null
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public static float? AsFloat(this string? x)
    {
        if (x == null) return null;

        if (float.TryParse(x, out var f)) return f;

        throw new InvalidDataException($"string {x} is not a valid float");
    }
}