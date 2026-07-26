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
    [Description("Lists the alerts configured on a maple grove (érablière): the sensor thresholds " +
                 "that trigger a notification, who gets notified, whether each alert is enabled and when it last triggered. " +
                 "Use this to answer questions about alerting, notification recipients or the last time an alert fired. " +
                 "The maple grove identifier comes from list_erablieres.")]
    public static async Task<IReadOnlyList<AlerteSummary>> GetAlertesAsync(
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

        return alertes.Select(AlerteSummary.From).ToArray();
    }
}
