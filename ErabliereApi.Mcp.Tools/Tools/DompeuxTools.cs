using System.ComponentModel;
using ErabliereAPI.Proxy;
using ErabliereApi.Mcp.Models;
using ModelContextProtocol.Server;

namespace ErabliereApi.Mcp.Tools;

/// <summary>
/// Read-only tools exposing the dumping events of a maple grove.
/// </summary>
[McpServerToolType]
public static class DompeuxTools
{
    /// <summary>
    /// Lists the dumping events of a maple grove over a date range.
    /// </summary>
    [McpServerTool(Name = "get_dompeux", ReadOnly = true, Idempotent = true)]
    [Description("Lists the dumping events (dompeux) of a maple grove: each time the sap collection tank was emptied, with the start, the end and the duration of the cycle. " +
                 "Use this to answer how often the tank was emptied, when the last emptying happened, or how long the cycles last — it is the production signal that is not a sensor reading. " +
                 "Returns an envelope {summary, data, truncated} with the events in chronological order.")]
    public static async Task<ToolResponse<IReadOnlyList<DompeuxSummary>>> GetDompeuxAsync(
        IErabliereAPIProxy proxy,
        [Description("Identifier (GUID) of the maple grove, as returned by list_erablieres.")]
        string erabliereId,
        [Description("Optional start of the range, inclusive. ISO 8601, for example 2026-03-12 or 2026-03-12T06:30:00-04:00. Omit to start at the first recorded event.")]
        string? startDate = null,
        [Description("Optional end of the range, inclusive. Same ISO 8601 format as startDate, and must not be earlier than it. Omit to stop at the last recorded event.")]
        string? endDate = null,
        [Description("Maximum number of events to return, between 1 and 100. Defaults to 25. The API returns the OLDEST events of the range first, so move startDate forward to look at recent activity.")]
        int? top = null,
        CancellationToken cancellationToken = default)
    {
        var id = ToolArguments.ParseId(erabliereId, nameof(erabliereId));
        var (start, end) = ToolArguments.ParseOptionalDateRange(startDate, endDate);
        var validatedTop = ToolArguments.ValidateTop(top);

        var dompeux = await proxy.DompeuxAllAsync(
            id,
            x_ddr: null,
            dd: start,
            df: end,
            q: validatedTop,
            // "c" is the ascending order and the default of the API. The
            // descending order is implemented server side as a Reverse() over an
            // unordered query, which is not something to depend on.
            o: "c",
            cancellationToken);

        var summaries = dompeux.Select(DompeuxSummary.From).ToArray();
        var truncated = summaries.Length == validatedTop;

        return ToolResponse.ForList(Describe(summaries, truncated), summaries, truncated);
    }

    private static string Describe(IReadOnlyList<DompeuxSummary> summaries, bool truncated)
    {
        if (summaries.Count == 0)
        {
            return "No dumping event in the requested range.";
        }

        var durations = summaries.Where(dompeux => dompeux.DureeSecondes.HasValue)
                                 .Select(dompeux => dompeux.DureeSecondes!.Value)
                                 .ToArray();

        var sentence = $"{summaries.Count} dumping events from {Format(summaries[0].T)} to {Format(summaries[^1].T)}";

        if (durations.Length > 0)
        {
            sentence += $", average cycle {Math.Round(durations.Average(), 1, MidpointRounding.AwayFromZero)} s";
        }

        sentence += ".";

        if (truncated)
        {
            sentence += " The list stops at the requested maximum, so more events may follow: raise top or move startDate forward.";
        }

        return sentence;
    }

    private static string Format(DateTimeOffset? date)
    {
        return date?.ToString(Serialization.Iso8601SecondsConverter.Format, System.Globalization.CultureInfo.InvariantCulture) ?? "?";
    }
}
