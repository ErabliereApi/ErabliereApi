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
                    datedf = ParseDateCourte(key, culture, DateTime.Now.Year);

                    hasDecember |= datedf.Value.Month == 12;
                    if (hasDecember && datedf.Value.Month == 1)
                    {
                        datedf = datedf.Value.AddYears(1);
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

    // Espace, espace insécable et espace insécable étroite : l'API et les cultures .NET
    // n'utilisent pas toutes l'espace ordinaire entre le jour et le mois.
    private static readonly char[] SeparateursDate = ['\u0020', '\u00A0', '\u202F'];

    /// <summary>
    /// Parse une date courte du service météo du gouvernement du Canada, p. ex. « sam, 18 avr »
    /// ou « sam, 1 août ».
    ///
    /// <para>
    /// <see cref="DateTime.ParseExact(string, string, IFormatProvider)"/> avec le format
    /// « ddd, d MMM » n'est pas utilisable : les abréviations de l'API n'ont pas de point final
    /// alors que celles de la culture en ont un pour certains mois seulement (« avr. » mais
    /// « mars », « mai », « juin », « août »). De plus, « ddd » force .NET à valider le jour de la
    /// semaine contre la date, ce qui échoue au changement d'année puisqu'on ne connaît pas encore
    /// l'année au moment du parsing.
    /// </para>
    /// </summary>
    /// <param name="date">La date courte telle que retournée par l'API</param>
    /// <param name="culture">Culture utilisée pour reconnaître le nom du mois</param>
    /// <param name="annee">Année à appliquer, l'API ne la fournit pas</param>
    public static DateTime ParseDateCourte(string date, CultureInfo culture, int annee)
    {
        var indexVirgule = date.IndexOf(',');
        var jourEtMois = (indexVirgule >= 0 ? date[(indexVirgule + 1)..] : date).Trim();

        var parties = jourEtMois.Split(SeparateursDate, StringSplitOptions.RemoveEmptyEntries);

        if (parties.Length == 2)
        {
            // Le français place le jour avant le mois, l'anglais l'inverse selon la culture.
            if (int.TryParse(parties[0], NumberStyles.Integer, culture, out var jour))
            {
                return new DateTime(annee, ParseMois(parties[1], culture), jour);
            }

            if (int.TryParse(parties[1], NumberStyles.Integer, culture, out jour))
            {
                return new DateTime(annee, ParseMois(parties[0], culture), jour);
            }
        }

        throw new FormatException($"La date '{date}' n'est pas dans le format attendu 'jjj, j mmm'.");
    }

    /// <summary>
    /// Associe le nom de mois retourné par l'API à son numéro, en tolérant l'absence de point
    /// final et une abréviation plus courte que celle de la culture (« juil » vs « juill. »).
    /// </summary>
    private static int ParseMois(string nomMois, CultureInfo culture)
    {
        var nom = nomMois.Trim().TrimEnd('.');

        if (nom.Length == 0)
        {
            throw new FormatException($"Le mois '{nomMois}' est vide.");
        }

        var comparaison = culture.CompareInfo;
        const CompareOptions options = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

        var noms = Enumerable.Range(1, 12)
            .Select(m => (Mois: m, Complet: culture.DateTimeFormat.GetMonthName(m),
                          Abrege: culture.DateTimeFormat.GetAbbreviatedMonthName(m).TrimEnd('.')))
            .ToArray();

        foreach (var (mois, complet, abrege) in noms)
        {
            if (comparaison.Compare(nom, complet, options) == 0 ||
                comparaison.Compare(nom, abrege, options) == 0)
            {
                return mois;
            }
        }

        var correspondances = noms
            .Where(n => comparaison.IsPrefix(n.Complet, nom, options) ||
                        comparaison.IsPrefix(n.Abrege, nom, options))
            .Select(n => n.Mois)
            .ToArray();

        if (correspondances.Length == 1)
        {
            return correspondances[0];
        }

        throw new FormatException(
            $"Le mois '{nomMois}' n'a pas pu être associé à un mois de la culture {culture.Name}.");
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