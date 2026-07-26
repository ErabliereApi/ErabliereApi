using System.Globalization;
using ErabliereAPI.Proxy;
using ErabliereApi.Mcp.Models;

namespace ErabliereApi.Mcp.Services;

/// <summary>
/// Turns the raw readings of a sensor into something a model can read.
/// </summary>
/// <remarks>
/// A sensor reporting every five minutes produces about 8 600 readings a month.
/// Handing that to a model is both useless and ruinous, so a range is always
/// reduced to its statistics plus a bounded series.
/// </remarks>
public static class DonneesCapteurSummarizer
{
    /// <summary>
    /// Default number of points in the downsampled series.
    /// </summary>
    public const int DefaultMaxSeriePoints = 100;

    /// <summary>
    /// Hard ceiling on the number of points in the downsampled series.
    /// </summary>
    public const int MaxSeriePointsLimit = 200;

    /// <summary>
    /// Summarizes the readings of one sensor.
    /// </summary>
    /// <param name="donnees">
    /// The readings as returned by the API. They may come in any order, the
    /// summarizer sorts them by timestamp.
    /// </param>
    /// <param name="unit">Unit symbol of the sensor, copied into the summary.</param>
    /// <param name="maxSeriePoints">Upper bound on the number of points in the series.</param>
    public static DonneesCapteurSummary Summarize(
        IEnumerable<GetDonneesCapteurV2> donnees,
        string? unit,
        int maxSeriePoints = DefaultMaxSeriePoints)
    {
        ArgumentNullException.ThrowIfNull(donnees);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxSeriePoints, 1);

        // A reading without a timestamp cannot be placed on a series, and one
        // without a value cannot be averaged. Both are dropped from the
        // statistics; the textual ones are still surfaced through LatestText.
        var ordered = donnees.Where(donnee => donnee.D.HasValue)
                             .OrderBy(donnee => donnee.D!.Value)
                             .ToArray();

        var numeric = ordered.Where(donnee => donnee.Valeur.HasValue).ToArray();

        if (numeric.Length == 0)
        {
            var lastText = ordered.LastOrDefault(donnee => !string.IsNullOrEmpty(donnee.Text));

            return new DonneesCapteurSummary(
                Count: 0,
                Unit: unit,
                Min: null,
                Max: null,
                Avg: null,
                Latest: null,
                First: ordered.FirstOrDefault()?.D,
                Last: ordered.LastOrDefault()?.D,
                LatestText: lastText?.Text,
                Serie: [],
                SerieIsDownsampled: false);
        }

        var values = Array.ConvertAll(numeric, donnee => donnee.Valeur!.Value);
        var serie = Downsample(numeric, maxSeriePoints);

        return new DonneesCapteurSummary(
            Count: numeric.Length,
            Unit: unit,
            Min: Round(values.Min()),
            Max: Round(values.Max()),
            Avg: Round(values.Average()),
            Latest: Round(values[^1]),
            First: numeric[0].D,
            Last: numeric[^1].D,
            LatestText: ordered.LastOrDefault(donnee => !string.IsNullOrEmpty(donnee.Text))?.Text,
            Serie: serie,
            SerieIsDownsampled: serie.Count < numeric.Length);
    }

    /// <summary>
    /// Writes the sentence put in the <c>summary</c> field of the envelope.
    /// </summary>
    /// <param name="summary">The statistics to describe.</param>
    /// <param name="capteurNom">Display name of the sensor.</param>
    /// <param name="truncated">True when the API capped the range.</param>
    public static string Describe(DonneesCapteurSummary summary, string? capteurNom, bool truncated)
    {
        ArgumentNullException.ThrowIfNull(summary);

        var nom = string.IsNullOrWhiteSpace(capteurNom) ? "The sensor" : $"Sensor '{capteurNom}'";

        if (summary.Count == 0)
        {
            return summary.LatestText is null
                ? $"{nom} has no reading in the requested range."
                : $"{nom} has no numeric reading in the requested range, its last textual value is '{summary.LatestText}'.";
        }

        var unit = string.IsNullOrWhiteSpace(summary.Unit) ? "" : $" {summary.Unit}";

        var sentence = string.Create(CultureInfo.InvariantCulture,
            $"{nom}: {summary.Count} readings from {Format(summary.First)} to {Format(summary.Last)}, min {summary.Min}{unit}, max {summary.Max}{unit}, average {summary.Avg}{unit}, latest {summary.Latest}{unit}.");

        if (summary.SerieIsDownsampled)
        {
            sentence += $" The serie is downsampled to {summary.Serie.Count} averaged points.";
        }

        if (truncated)
        {
            sentence += " The API capped the query, so these statistics only cover the beginning of the requested range: narrow startDate and endDate to get the whole picture.";
        }

        return sentence;
    }

    private static string Format(DateTimeOffset? date)
    {
        return date?.ToString(Serialization.Iso8601SecondsConverter.Format, CultureInfo.InvariantCulture) ?? "?";
    }

    private static double Round(double value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Reduces the readings to at most <paramref name="maxPoints"/> points by
    /// averaging contiguous buckets of equal size.
    /// </summary>
    private static IReadOnlyList<SeriePoint> Downsample(GetDonneesCapteurV2[] numeric, int maxPoints)
    {
        if (numeric.Length <= maxPoints)
        {
            return Array.ConvertAll(numeric, donnee => new SeriePoint(donnee.D!.Value, Round(donnee.Valeur!.Value)));
        }

        var points = new SeriePoint[maxPoints];

        for (var bucket = 0; bucket < maxPoints; bucket++)
        {
            // Computed on longs so the multiplication cannot overflow on a long
            // range, and so the buckets stay balanced instead of leaving the
            // remainder on the last one.
            var start = (int)((long)bucket * numeric.Length / maxPoints);
            var end = (int)((long)(bucket + 1) * numeric.Length / maxPoints);

            var sum = 0d;

            for (var index = start; index < end; index++)
            {
                sum += numeric[index].Valeur!.Value;
            }

            // The bucket is timestamped with its first reading, so the series
            // stays strictly increasing and its first point is the real start of
            // the range.
            points[bucket] = new SeriePoint(numeric[start].D!.Value, Round(sum / (end - start)));
        }

        return points;
    }
}
