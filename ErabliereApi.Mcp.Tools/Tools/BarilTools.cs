using System.ComponentModel;
using ErabliereAPI.Proxy;
using ErabliereApi.Mcp.Models;
using ModelContextProtocol.Server;

namespace ErabliereApi.Mcp.Tools;

/// <summary>
/// Read-only tools exposing the barrels of syrup of a maple grove.
/// </summary>
[McpServerToolType]
public static class BarilTools
{
    /// <summary>
    /// Lists the barrels closed over a date range.
    /// </summary>
    [McpServerTool(Name = "get_barils", ReadOnly = true, Idempotent = true)]
    [Description("Lists the barrels of syrup of a maple grove with the date each was closed, its estimated grade and its grade after classification. " +
                 "Use this for questions about production volume over a season, or about the quality of the syrup and how the estimate compared to the classification. " +
                 "Returns an envelope {summary, data, truncated} whose summary already counts the barrels per grade.")]
    public static async Task<ToolResponse<IReadOnlyList<BarilSummary>>> GetBarilsAsync(
        IErabliereAPIProxy proxy,
        [Description("Identifier (GUID) of the maple grove, as returned by list_erablieres.")]
        string erabliereId,
        [Description("Optional start of the range, inclusive, matched against the closing date. ISO 8601, for example 2026-03-01. Omit to start at the first barrel.")]
        string? startDate = null,
        [Description("Optional end of the range, inclusive. Same ISO 8601 format as startDate, and must not be earlier than it. Omit to stop at the last barrel.")]
        string? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var id = ToolArguments.ParseId(erabliereId, nameof(erabliereId));
        var (start, end) = ToolArguments.ParseOptionalDateRange(startDate, endDate);

        // This endpoint takes no quantity parameter, so the whole range comes
        // back and the envelope is what bounds the answer.
        var barils = await proxy.BarilAllAsync(id, dd: start, df: end, cancellationToken);

        var summaries = barils.Select(BarilSummary.From)
                              .OrderBy(baril => baril.Df)
                              .ToArray();

        return ToolResponse.ForList(Describe(summaries), summaries);
    }

    private static string Describe(IReadOnlyList<BarilSummary> summaries)
    {
        if (summaries.Count == 0)
        {
            return "No barrel in the requested range.";
        }

        // The grade after classification is the authoritative one; the estimate
        // stands in while the barrel is not classified yet.
        var grades = summaries.GroupBy(baril => baril.Q ?? baril.Qe ?? "unknown")
                              .OrderByDescending(group => group.Count())
                              .Select(group => $"{group.Count()} {group.Key}");

        return $"{summaries.Count} barrels closed between {Format(summaries[0].Df)} and {Format(summaries[^1].Df)}, by grade: {string.Join(", ", grades)}.";
    }

    private static string Format(DateTimeOffset? date)
    {
        return date?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) ?? "?";
    }
}
