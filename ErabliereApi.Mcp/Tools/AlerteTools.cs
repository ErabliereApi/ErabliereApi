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
}
