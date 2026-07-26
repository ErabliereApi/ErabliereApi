using System.Text.Json.Serialization;

namespace ErabliereApi.Mcp.Models;

/// <summary>
/// One point of a downsampled sensor series.
/// </summary>
/// <param name="T">Timestamp of the bucket, ISO 8601 with the original offset.</param>
/// <param name="V">Mean of the readings falling in the bucket, rounded to two decimals.</param>
public record SeriePoint(
    [property: JsonPropertyName("t")] DateTimeOffset T,
    [property: JsonPropertyName("v")] double V);

/// <summary>
/// What a model gets instead of a raw dump of sensor readings: the statistics
/// that answer most questions, plus a series short enough to read.
/// </summary>
/// <param name="Count">Number of readings the statistics were computed on.</param>
/// <param name="Unit">Unit symbol of every value in this object, null when the sensor has none.</param>
/// <param name="Min">Lowest reading of the range.</param>
/// <param name="Max">Highest reading of the range.</param>
/// <param name="Avg">Mean of the readings, rounded to two decimals.</param>
/// <param name="Latest">Most recent reading of the range.</param>
/// <param name="First">Timestamp of the oldest reading of the range.</param>
/// <param name="Last">Timestamp of the most recent reading of the range.</param>
/// <param name="LatestText">
/// Textual value of the most recent reading, for the sensors reporting a label
/// rather than a number. Null on a numeric sensor.
/// </param>
/// <param name="Serie">
/// The readings downsampled to at most the requested number of points. Each point
/// is the mean of a contiguous bucket, so a spike can be smoothed out: the true
/// extremes are always in <paramref name="Min"/> and <paramref name="Max"/>.
/// </param>
/// <param name="SerieIsDownsampled">True when a bucket holds more than one reading.</param>
public record DonneesCapteurSummary(
    [property: JsonPropertyName("count")] int Count,
    [property: JsonPropertyName("unit")] string? Unit,
    [property: JsonPropertyName("min")] double? Min,
    [property: JsonPropertyName("max")] double? Max,
    [property: JsonPropertyName("avg")] double? Avg,
    [property: JsonPropertyName("latest")] double? Latest,
    [property: JsonPropertyName("first")] DateTimeOffset? First,
    [property: JsonPropertyName("last")] DateTimeOffset? Last,
    [property: JsonPropertyName("latestText")] string? LatestText,
    [property: JsonPropertyName("serie")] IReadOnlyList<SeriePoint> Serie,
    [property: JsonPropertyName("serieIsDownsampled")] bool SerieIsDownsampled);
