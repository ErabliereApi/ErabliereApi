using System.ComponentModel;
using ErabliereAPI.Proxy;
using ErabliereApi.Mcp.Models;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace ErabliereApi.Mcp.Tools;

/// <summary>
/// Read-only tools exposing the maple groves (érablières) of the account
/// owning the configured api key.
/// </summary>
[McpServerToolType]
public static class ErabliereTools
{
    /// <summary>
    /// Lists the maple groves reachable with the configured api key.
    /// </summary>
    [McpServerTool(Name = "list_erablieres", ReadOnly = true, Idempotent = true)]
    [Description("Lists the maple groves (érablières) the configured API key can read, with their name, address and region. " +
                 "Call this first: every other tool of this server needs a maple grove identifier, and this is the only way to obtain one. " +
                 "Returns an envelope {summary, data, truncated}.")]
    public static async Task<ToolResponse<IReadOnlyList<ErabliereSummary>>> ListErablieresAsync(
        IErabliereAPIProxy proxy,
        [Description("Optional case-sensitive substring to search in the maple grove name, for example 'Sucrerie'. Omit to list them all.")]
        string? nameContains = null,
        [Description("Maximum number of maple groves to return, between 1 and 100. Defaults to 25.")]
        int? top = null,
        CancellationToken cancellationToken = default)
    {
        var validatedTop = ToolArguments.ValidateTop(top);
        var filter = ToolArguments.BuildContainsFilter("nom", nameContains);

        var erablieres = await proxy.ErablieresAllAsync(
            orderby: null,
            select: null,
            filter: filter,
            top: validatedTop,
            skip: null,
            count: null,
            expand: null,
            cancellationToken);

        var summaries = erablieres.Select(ErabliereSummary.From).ToArray();

        var summary = summaries.Length switch
        {
            0 when filter is null => "The configured API key can read no maple grove at all; check the key has been granted access to one.",
            0 => "No maple grove matches this name.",
            _ => $"{summaries.Length} maple groves: {string.Join(", ", summaries.Select(erabliere => erabliere.Nom))}."
        };

        return ToolResponse.ForList(summary, summaries, truncated: summaries.Length == validatedTop);
    }

    /// <summary>
    /// Gets a single maple grove by identifier.
    /// </summary>
    /// <remarks>
    /// ErabliereAPI has no GET /Erablieres/{id} endpoint, so the lookup goes
    /// through the OData filter of the list endpoint.
    /// </remarks>
    [McpServerTool(Name = "get_erabliere", ReadOnly = true, Idempotent = true)]
    [Description("Gets the details of a single maple grove (érablière) by its identifier: name, description, address, postal code, region and visibility. " +
                 "Use this when the identifier is already known and only that one maple grove matters; prefer list_erablieres when several are in play. " +
                 "Returns an envelope {summary, data, truncated}.")]
    public static async Task<ToolResponse<ErabliereSummary>> GetErabliereAsync(
        IErabliereAPIProxy proxy,
        [Description("Identifier (GUID) of the maple grove, as returned by list_erablieres.")]
        string erabliereId,
        CancellationToken cancellationToken = default)
    {
        var id = ToolArguments.ParseId(erabliereId, nameof(erabliereId));

        var erablieres = await proxy.ErablieresAllAsync(
            orderby: null,
            select: null,
            filter: $"id eq {id}",
            top: 1,
            skip: null,
            count: null,
            expand: null,
            cancellationToken);

        var erabliere = erablieres.FirstOrDefault();

        if (erabliere is null)
        {
            throw new McpException($"No maple grove found with the identifier {id}. It may not exist, or the configured API key may not have access to it. Call list_erablieres to see the available ones.");
        }

        var summary = ErabliereSummary.From(erabliere);

        return ToolResponse.ForItem($"Maple grove '{summary.Nom}'{(string.IsNullOrWhiteSpace(summary.RegionAdministrative) ? "" : $" in {summary.RegionAdministrative}")}.", summary);
    }
}
