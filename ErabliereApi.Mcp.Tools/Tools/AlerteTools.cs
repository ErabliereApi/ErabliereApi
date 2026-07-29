using System.ComponentModel;
using ErabliereAPI.Proxy;
using ErabliereApi.Mcp.Models;
using ModelContextProtocol.Server;

namespace ErabliereApi.Mcp.Tools;

/// <summary>
/// Read-only tools exposing the alerts configured on a maple grove.
/// </summary>
[McpServerToolType]
public static class AlerteTools
{
    /// <summary>
    /// Lists the alerts of a maple grove.
    /// </summary>
    [McpServerTool(Name = "get_alertes", ReadOnly = true, Idempotent = true)]
    [Description("Lists the alerts configured on a maple grove (érablière): the temperature, vacuum and tank level thresholds that trigger a notification, who gets notified, whether each alert is enabled and when it last fired. " +
                 "Use this to answer what is being monitored, who would be warned, or when an alert last triggered — not to read the measurements themselves, which is get_donnees_capteur. " +
                 "Returns an envelope {summary, data, truncated}.")]
    public static async Task<ToolResponse<IReadOnlyList<AlerteSummary>>> GetAlertesAsync(
        IErabliereAPIProxy proxy,
        [Description("Identifier (GUID) of the maple grove whose alerts must be listed, as returned by list_erablieres.")]
        string erabliereId,
        [Description("Maximum number of alerts to return, between 1 and 100. Defaults to 25.")]
        int? top = null,
        CancellationToken cancellationToken = default)
    {
        var id = ToolArguments.ParseId(erabliereId, nameof(erabliereId));
        var validatedTop = ToolArguments.ValidateTop(top);

        var alertes = await proxy.AlertesAllAsync(
            id,
            select: null,
            filter: null,
            top: validatedTop,
            skip: null,
            count: null,
            expand: null,
            orderby: null,
            cancellationToken);

        var summaries = alertes.Select(AlerteSummary.From).ToArray();

        var enabled = summaries.Count(alerte => alerte.IsEnable == true);
        var summary = summaries.Length == 0
            ? "This maple grove has no configured alert."
            : $"{summaries.Length} alerts, {enabled} of them enabled.";

        return ToolResponse.ForList(summary, summaries, truncated: summaries.Length == validatedTop);
    }

    /// <summary>
    /// Lists the sensor alerts of a maple grove.
    /// </summary>
    [McpServerTool(Name = "get_alertes_capteur", ReadOnly = true, Idempotent = true)]
    [Description("Lists the sensor alerts of a maple grove (érablière): for each watched sensor, the minimum and maximum reading that fires a notification, who gets notified, whether the alert is enabled and when it last fired. " +
                 "Use this to answer which sensors are being watched and at what threshold. The bounds are expressed in the unit of their own sensor, returned as capteurSymbole, so they compare directly with what get_donnees_capteur reports. " +
                 "get_alertes is the neighbouring tool: it covers the alerts configured on the maple grove itself rather than on one sensor. Returns an envelope {summary, data, truncated}.")]
    public static async Task<ToolResponse<IReadOnlyList<AlerteCapteurSummary>>> GetAlertesCapteurAsync(
        IErabliereAPIProxy proxy,
        [Description("Identifier (GUID) of the maple grove whose sensor alerts must be listed, as returned by list_erablieres.")]
        string erabliereId,
        [Description("Maximum number of sensor alerts to return, between 1 and 100. Defaults to 25.")]
        int? top = null,
        CancellationToken cancellationToken = default)
    {
        var id = ToolArguments.ParseId(erabliereId, nameof(erabliereId));
        var validatedTop = ToolArguments.ValidateTop(top);

        // The sensor is always included: a bound of 12 means nothing without the
        // name and the unit of what it watches, and this is the very call the web
        // application makes on this route.
        var alertes = await proxy.AlertesCapteurAsync(id, include: "Capteur", cancellationToken);

        // The endpoint applies neither an order nor a limit of its own, so both are
        // done here: grouping the alerts by sensor makes the list readable, and a
        // stable order makes the tail that gets cut off predictable.
        var ordered = alertes.Select(AlerteCapteurSummary.From)
                             .OrderBy(alerte => alerte.CapteurNom, StringComparer.OrdinalIgnoreCase)
                             .ThenBy(alerte => alerte.Nom, StringComparer.OrdinalIgnoreCase)
                             .ToArray();

        var summaries = ordered.Take(validatedTop).ToArray();
        var truncated = ordered.Length > summaries.Length;

        var enabled = summaries.Count(alerte => alerte.IsEnable == true);
        var capteurs = summaries.Select(alerte => alerte.IdCapteur).Distinct().Count();

        var summary = summaries.Length == 0
            ? "No sensor of this maple grove has a configured alert."
            : $"{summaries.Length} sensor alerts on {capteurs} sensors, {enabled} of them enabled.";

        if (truncated)
        {
            summary += $" This maple grove has {ordered.Length} sensor alerts in total; raise 'top' to see the rest.";
        }

        return ToolResponse.ForList(summary, summaries, truncated);
    }
}
