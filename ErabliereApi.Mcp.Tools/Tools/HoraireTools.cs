using System.ComponentModel;
using ErabliereAPI.Proxy;
using ErabliereApi.Mcp.Models;
using ModelContextProtocol.Server;

namespace ErabliereApi.Mcp.Tools;

/// <summary>
/// Read-only tool exposing the opening hours of a maple grove.
/// </summary>
[McpServerToolType]
public static class HoraireTools
{
    /// <summary>
    /// Gets the weekly opening hours of a maple grove.
    /// </summary>
    [McpServerTool(Name = "get_horaire", ReadOnly = true, Idempotent = true)]
    [Description("Gets the weekly opening hours of a maple grove: one \"HH:mm-HH:mm\" range per day of the week, or null on the days it is closed. " +
                 "Use this to answer when a maple grove welcomes visitors. Returns an envelope {summary, data, truncated} where data is null when no schedule was ever recorded.")]
    public static async Task<ToolResponse<HoraireSummary?>> GetHoraireAsync(
        IErabliereAPIProxy proxy,
        [Description("Identifier (GUID) of the maple grove, as returned by list_erablieres.")]
        string erabliereId,
        CancellationToken cancellationToken = default)
    {
        var id = ToolArguments.ParseId(erabliereId, nameof(erabliereId));

        // The endpoint returns a collection although a maple grove has at most one
        // schedule.
        var horaires = await proxy.HoraireAllAsync(id, cancellationToken);
        var horaire = horaires.FirstOrDefault();

        if (horaire is null)
        {
            return ToolResponse.ForItem<HoraireSummary?>("This maple grove has no recorded opening hours.", null);
        }

        var summary = HoraireSummary.From(horaire);

        var openDays = new[]
        {
            ("Monday", summary.Lundi),
            ("Tuesday", summary.Mardi),
            ("Wednesday", summary.Mercredi),
            ("Thursday", summary.Jeudi),
            ("Friday", summary.Vendredi),
            ("Saturday", summary.Samedi),
            ("Sunday", summary.Dimanche)
        }
        .Where(day => !string.IsNullOrWhiteSpace(day.Item2))
        .Select(day => $"{day.Item1} {day.Item2}")
        .ToArray();

        var sentence = openDays.Length == 0
            ? "This maple grove is recorded as closed every day of the week."
            : $"Open {string.Join(", ", openDays)}.";

        return ToolResponse.ForItem<HoraireSummary?>(sentence, summary);
    }
}
