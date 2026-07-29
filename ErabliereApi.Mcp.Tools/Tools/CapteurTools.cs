using System.ComponentModel;
using ErabliereAPI.Proxy;
using ErabliereApi.Mcp.Models;
using ErabliereApi.Mcp.Services;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace ErabliereApi.Mcp.Tools;

/// <summary>
/// Read-only tools exposing the sensors of a maple grove and their readings.
/// </summary>
[McpServerToolType]
public static class CapteurTools
{
    /// <summary>
    /// Number of readings fetched from the API before summarizing.
    /// </summary>
    /// <remarks>
    /// The API applies no cap of its own when 'top' is omitted, so a wide range on
    /// a sensor reporting every five minutes would download a whole season. This
    /// bound keeps the call cheap; hitting it flags the response as truncated
    /// instead of quietly summarizing a partial window.
    /// </remarks>
    public const int MaxFetchedDonnees = 5000;

    /// <summary>
    /// Lists the sensors of a maple grove.
    /// </summary>
    [McpServerTool(Name = "list_capteurs", ReadOnly = true, Idempotent = true)]
    [Description("Lists the sensors of a maple grove with their unit, kind, connectivity and battery level. " +
                 "Use this before get_donnees_capteur, which needs a sensor identifier, or to answer whether a sensor is online or running low on battery. " +
                 "Returns an envelope {summary, data, truncated}.")]
    public static async Task<ToolResponse<IReadOnlyList<CapteurSummary>>> ListCapteursAsync(
        IErabliereAPIProxy proxy,
        [Description("Identifier (GUID) of the maple grove, as returned by list_erablieres.")]
        string erabliereId,
        [Description("Optional case-sensitive substring to search in the sensor name, for example 'Température'. Omit to list them all.")]
        string? nameContains = null,
        [Description("Maximum number of sensors to return, between 1 and 100. Defaults to 25.")]
        int? top = null,
        CancellationToken cancellationToken = default)
    {
        var id = ToolArguments.ParseId(erabliereId, nameof(erabliereId));
        var validatedTop = ToolArguments.ValidateTop(top);

        var capteurs = await proxy.CapteursAllAsync(
            id,
            filtreNom: string.IsNullOrWhiteSpace(nameContains) ? null : nameContains.Trim(),
            select: null,
            filter: null,
            top: validatedTop,
            skip: null,
            count: null,
            expand: null,
            orderby: "indiceOrdre",
            cancellationToken);

        var summaries = capteurs.Select(CapteurSummary.From).ToArray();

        var offline = summaries.Count(capteur => capteur.Online == false);
        var summary = summaries.Length == 0
            ? "This maple grove has no sensor matching the query."
            : $"{summaries.Length} sensors, {offline} of them offline.";

        return ToolResponse.ForList(summary, summaries, truncated: summaries.Length == validatedTop);
    }

    /// <summary>
    /// Summarizes the readings of one sensor over a date range.
    /// </summary>
    [McpServerTool(Name = "get_donnees_capteur", ReadOnly = true, Idempotent = true)]
    [Description("Summarizes the readings of one sensor over a mandatory date range: count, min, max, average, latest value, and a series downsampled to at most 100 averaged points. " +
                 "Use this for any question about what a sensor measured — a temperature trend, a vacuum drop, a tank level over a week. It never returns the raw readings, so ask for the range you actually need. " +
                 "Get the identifiers from list_erablieres then list_capteurs. Returns an envelope {summary, data, truncated}; when truncated is true the range held more readings than could be read at once, so narrow it.")]
    public static async Task<ToolResponse<DonneesCapteurSummary>> GetDonneesCapteurAsync(
        IErabliereAPIProxy proxy,
        [Description("Identifier (GUID) of the maple grove owning the sensor, as returned by list_erablieres.")]
        string erabliereId,
        [Description("Identifier (GUID) of the sensor to read, as returned by list_capteurs.")]
        string capteurId,
        [Description("Start of the range, inclusive. Required. ISO 8601, for example 2026-03-12 or 2026-03-12T06:30:00-04:00. A date without a time is read as midnight local time.")]
        string startDate,
        [Description("End of the range, inclusive. Required. Same ISO 8601 format as startDate, and must not be earlier than it.")]
        string endDate,
        [Description("Maximum number of points in the downsampled series, between 1 and 200. Defaults to 100. Lower it when only the statistics matter.")]
        int? maxPoints = null,
        CancellationToken cancellationToken = default)
    {
        var idErabliere = ToolArguments.ParseId(erabliereId, nameof(erabliereId));
        var idCapteur = ToolArguments.ParseId(capteurId, nameof(capteurId));
        var range = ToolArguments.ParseRequiredDateRange(startDate, endDate);
        var validatedMaxPoints = ToolArguments.ValidateMaxPoints(maxPoints);

        // Read the sensor first: it carries the unit the readings are expressed
        // in, and it is what tells apart "this sensor recorded nothing" from
        // "this sensor does not exist".
        var capteur = await GetCapteurAsync(proxy, idErabliere, idCapteur, cancellationToken);

        var donnees = await proxy.DonneesCapteurV2AllAsync(
            idCapteur,
            x_ddr: null,
            dd: range.Start,
            df: range.End,
            order: "asc",
            top: MaxFetchedDonnees,
            cancellationToken);

        var truncated = donnees.Count >= MaxFetchedDonnees;
        var summary = DonneesCapteurSummarizer.Summarize(donnees, capteur.Symbole, validatedMaxPoints);

        return ToolResponse.ForItem(
            DonneesCapteurSummarizer.Describe(summary, capteur.Nom, truncated),
            summary,
            truncated);
    }

    private static async Task<Capteur> GetCapteurAsync(
        IErabliereAPIProxy proxy,
        Guid idErabliere,
        Guid idCapteur,
        CancellationToken cancellationToken)
    {
        try
        {
            return await proxy.CapteursGETAsync(idErabliere, idCapteur, cancellationToken);
        }
        catch (ApiException exception) when (exception.StatusCode is 400 or 403 or 404)
        {
            // The ownership filter answers 404 for an unknown sensor and 403 for
            // one belonging to somebody else. Both mean the same thing here.
            throw new McpException($"No sensor {idCapteur} readable on the maple grove {idErabliere}. Call list_capteurs to see the sensors of that maple grove, and check the sensor really belongs to it.");
        }
    }
}
