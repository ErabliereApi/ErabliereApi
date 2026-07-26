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
    [Description("Lists the maple groves (érablières) the configured API key can read. " +
                 "Use this first to discover the identifier of a maple grove before calling any other tool, " +
                 "or when the user asks what maple groves exist. Optionally filters on the name.")]
    public static async Task<IReadOnlyList<ErabliereSummary>> ListErablieresAsync(
        IErabliereAPIProxy proxy,
        [Description("Optional case-sensitive substring to search in the maple grove name. Omit to list them all.")]
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

        return erablieres.Select(ErabliereSummary.From).ToArray();
    }

    /// <summary>
    /// Gets a single maple grove by identifier.
    /// </summary>
    /// <remarks>
    /// ErabliereAPI has no GET /Erablieres/{id} endpoint, so the lookup goes
    /// through the OData filter of the list endpoint.
    /// </remarks>
    [McpServerTool(Name = "get_erabliere", ReadOnly = true, Idempotent = true)]
    [Description("Gets the details of a single maple grove (érablière) by its identifier. " +
                 "Use this when you already know the identifier, typically obtained from list_erablieres.")]
    public static async Task<ErabliereSummary> GetErabliereAsync(
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

        return ErabliereSummary.From(erabliere);
    }
}
