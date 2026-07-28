using System.Globalization;
using ModelContextProtocol;

namespace ErabliereApi.Mcp.Tools;

/// <summary>
/// A validated, non empty date range.
/// </summary>
/// <param name="Start">Inclusive lower bound.</param>
/// <param name="End">Inclusive upper bound.</param>
public readonly record struct DateRange(DateTimeOffset Start, DateTimeOffset End)
{
    /// <summary>
    /// Length of the range.
    /// </summary>
    public TimeSpan Duration => End - Start;
}

/// <summary>
/// Validation helpers shared by the MCP tools. Arguments come from a language
/// model, so they are validated defensively and rejected with an actionable
/// message instead of being forwarded to the API.
/// </summary>
public static class ToolArguments
{
    /// <summary>
    /// Default number of items returned when the caller does not specify one.
    /// </summary>
    public const int DefaultTop = 25;

    /// <summary>
    /// Hard upper bound on the number of items returned by a single tool call,
    /// to keep the result small enough for a model context.
    /// </summary>
    public const int MaxTop = 100;

    /// <summary>
    /// Parses an identifier supplied by the model.
    /// </summary>
    /// <exception cref="McpException">The value is missing or is not a valid GUID.</exception>
    public static Guid ParseId(string? value, string argumentName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new McpException($"The argument '{argumentName}' is required.");
        }

        if (!Guid.TryParse(value.Trim(), out var id))
        {
            throw new McpException($"The argument '{argumentName}' must be a GUID, for example 3fa85f64-5717-4562-b3fc-2c963f66afa6. Received: '{value}'.");
        }

        if (id == Guid.Empty)
        {
            throw new McpException($"The argument '{argumentName}' must not be an empty GUID.");
        }

        return id;
    }

    /// <summary>
    /// Validates the number of items requested by the model.
    /// </summary>
    /// <exception cref="McpException">The value is outside of the allowed range.</exception>
    public static int ValidateTop(int? top)
    {
        if (top is null)
        {
            return DefaultTop;
        }

        if (top < 1 || top > MaxTop)
        {
            throw new McpException($"The argument 'top' must be between 1 and {MaxTop}. Received: {top}.");
        }

        return top.Value;
    }

    /// <summary>
    /// Parses one optional date supplied by the model.
    /// </summary>
    /// <returns>Null when the value is absent.</returns>
    /// <exception cref="McpException">The value is not a readable date.</exception>
    public static DateTimeOffset? ParseOptionalDate(string? value, string argumentName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        // AssumeLocal rather than AssumeUniversal: a model that writes
        // "2026-03-12" means that day at the maple grove, not in UTC.
        if (!DateTimeOffset.TryParse(value.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date))
        {
            throw new McpException($"The argument '{argumentName}' must be an ISO 8601 date, for example 2026-03-12 or 2026-03-12T06:30:00-04:00. Received: '{value}'.");
        }

        return date;
    }

    /// <summary>
    /// Parses a mandatory date range supplied by the model.
    /// </summary>
    /// <exception cref="McpException">
    /// One of the bounds is missing or unreadable, or the range is inverted.
    /// </exception>
    public static DateRange ParseRequiredDateRange(string? startDate, string? endDate, string startArgumentName = "startDate", string endArgumentName = "endDate")
    {
        if (string.IsNullOrWhiteSpace(startDate) || string.IsNullOrWhiteSpace(endDate))
        {
            throw new McpException($"The arguments '{startArgumentName}' and '{endArgumentName}' are both required: this tool never reads an unbounded range, because a sensor can hold years of readings. Pass an ISO 8601 date, for example 2026-03-12 or 2026-03-12T06:30:00-04:00.");
        }

        var start = ParseOptionalDate(startDate, startArgumentName)!.Value;
        var end = ParseOptionalDate(endDate, endArgumentName)!.Value;

        if (end < start)
        {
            throw new McpException($"The argument '{endArgumentName}' ({end:yyyy-MM-dd HH:mm}) must not be earlier than '{startArgumentName}' ({start:yyyy-MM-dd HH:mm}).");
        }

        return new DateRange(start, end);
    }

    /// <summary>
    /// Parses an optional date range supplied by the model. Either bound may be
    /// left out, the API then reads it as unbounded on that side.
    /// </summary>
    /// <exception cref="McpException">A bound is unreadable, or the range is inverted.</exception>
    public static (DateTimeOffset? Start, DateTimeOffset? End) ParseOptionalDateRange(
        string? startDate,
        string? endDate,
        string startArgumentName = "startDate",
        string endArgumentName = "endDate")
    {
        var start = ParseOptionalDate(startDate, startArgumentName);
        var end = ParseOptionalDate(endDate, endArgumentName);

        if (start is not null && end is not null && end < start)
        {
            throw new McpException($"The argument '{endArgumentName}' ({end:yyyy-MM-dd HH:mm}) must not be earlier than '{startArgumentName}' ({start:yyyy-MM-dd HH:mm}).");
        }

        return (start, end);
    }

    /// <summary>
    /// Validates the number of points the model asked for in a downsampled
    /// series.
    /// </summary>
    /// <exception cref="McpException">The value is outside of the allowed range.</exception>
    public static int ValidateMaxPoints(int? maxPoints)
    {
        if (maxPoints is null)
        {
            return Services.DonneesCapteurSummarizer.DefaultMaxSeriePoints;
        }

        if (maxPoints < 1 || maxPoints > Services.DonneesCapteurSummarizer.MaxSeriePointsLimit)
        {
            throw new McpException($"The argument 'maxPoints' must be between 1 and {Services.DonneesCapteurSummarizer.MaxSeriePointsLimit}. Received: {maxPoints}.");
        }

        return maxPoints.Value;
    }

    /// <summary>
    /// Builds an OData 'contains' filter on the given property, escaping the
    /// single quotes so a search term cannot break out of the string literal.
    /// Returns null when no search term was provided.
    /// </summary>
    public static string? BuildContainsFilter(string propertyName, string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return null;
        }

        return $"contains({propertyName},'{EscapeODataString(searchTerm.Trim())}')";
    }

    /// <summary>
    /// Builds an OData filter matching a search term against any of the given
    /// properties. Returns null when no search term was provided.
    /// </summary>
    public static string? BuildContainsAnyFilter(string? searchTerm, params string[] propertyNames)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return null;
        }

        var escaped = EscapeODataString(searchTerm.Trim());

        return string.Join(" or ", propertyNames.Select(name => $"contains({name},'{escaped}')"));
    }

    /// <summary>
    /// Doubles the single quotes so a search term cannot break out of an OData
    /// string literal and be read as an expression.
    /// </summary>
    public static string EscapeODataString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return value.Replace("'", "''", StringComparison.Ordinal);
    }
}
