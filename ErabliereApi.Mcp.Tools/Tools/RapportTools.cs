using System.ComponentModel;
using ErabliereAPI.Proxy;
using ErabliereApi.Mcp.Models;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace ErabliereApi.Mcp.Tools;

/// <summary>
/// Read-only tools exposing the saved reports of a maple grove.
/// </summary>
[McpServerToolType]
public static class RapportTools
{
    /// <summary>
    /// Lists the saved reports of a maple grove.
    /// </summary>
    [McpServerTool(Name = "list_rapports", ReadOnly = true, Idempotent = true)]
    [Description("Lists the reports saved on a maple grove with their period and their aggregates, mostly degree-day reports used to anticipate the run. " +
                 "Use this to discover which reports exist and get their identifiers; the rows of a report are left out here, call get_rapport for those. " +
                 "Returns an envelope {summary, data, truncated}.")]
    public static async Task<ToolResponse<IReadOnlyList<RapportSummary>>> ListRapportsAsync(
        IErabliereAPIProxy proxy,
        [Description("Identifier (GUID) of the maple grove, as returned by list_erablieres.")]
        string erabliereId,
        [Description("Maximum number of reports to return, between 1 and 100. Defaults to 25.")]
        int? top = null,
        CancellationToken cancellationToken = default)
    {
        var id = ToolArguments.ParseId(erabliereId, nameof(erabliereId));
        var validatedTop = ToolArguments.ValidateTop(top);

        var rapports = await proxy.RapportsAllAsync(
            id,
            select: null,
            filter: null,
            top: validatedTop,
            skip: null,
            count: null,
            expand: null,
            orderby: "dateFin desc",
            cancellationToken);

        var summaries = rapports.Select(rapport => RapportSummary.From(rapport, includeDonnees: false)).ToArray();
        var truncated = summaries.Length == validatedTop;

        var summary = summaries.Length == 0
            ? "This maple grove has no saved report."
            : $"{summaries.Length} saved reports, the most recent covering up to {Format(summaries[0].DateFin)}.";

        return ToolResponse.ForList(summary, summaries, truncated);
    }

    /// <summary>
    /// Gets one saved report with its rows.
    /// </summary>
    [McpServerTool(Name = "get_rapport", ReadOnly = true, Idempotent = true)]
    [Description("Gets one saved report with its aggregates and its daily rows (date, mean, sum, min, max). " +
                 "Use this when the user asks about the content or the evolution of a specific report, after finding its identifier with list_rapports. " +
                 "Returns an envelope {summary, data, truncated}; a long season may have more rows than fit, in which case truncated is true.")]
    public static async Task<ToolResponse<RapportSummary>> GetRapportAsync(
        IErabliereAPIProxy proxy,
        [Description("Identifier (GUID) of the maple grove owning the report, as returned by list_erablieres.")]
        string erabliereId,
        [Description("Identifier (GUID) of the report to read, as returned by list_rapports.")]
        string rapportId,
        CancellationToken cancellationToken = default)
    {
        var idErabliere = ToolArguments.ParseId(erabliereId, nameof(erabliereId));
        var idRapport = ToolArguments.ParseId(rapportId, nameof(rapportId));

        Rapport rapport;

        try
        {
            rapport = await proxy.RapportsGETAsync(idErabliere, idRapport, cancellationToken);
        }
        catch (ApiException exception) when (exception.StatusCode is 400 or 403 or 404)
        {
            throw new McpException($"No report {idRapport} readable on the maple grove {idErabliere}. Call list_rapports to see the reports of that maple grove.");
        }

        var summary = RapportSummary.From(rapport, includeDonnees: true);
        var rowCount = summary.Donnees?.Count ?? 0;

        var sentence = string.Create(System.Globalization.CultureInfo.InvariantCulture,
            $"Report '{summary.Nom}' ({summary.Type}) from {Format(summary.DateDebut)} to {Format(summary.DateFin)}: {rowCount} rows, sum {summary.Somme}, average {summary.Moyenne}, min {summary.Min}, max {summary.Max}.");

        // The rows are the bulk of this payload and they are ordered by date, so
        // trimming the tail keeps a coherent, if shorter, period.
        var trimmed = ToolResponse.ForList(sentence, summary.Donnees ?? [], truncated: false);

        return ToolResponse.ForItem(
            trimmed.Summary,
            summary with { Donnees = trimmed.Data },
            trimmed.Truncated);
    }

    private static string Format(DateTimeOffset? date)
    {
        return date?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) ?? "?";
    }
}
