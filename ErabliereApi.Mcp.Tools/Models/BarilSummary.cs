using System.Text.Json.Serialization;
using ErabliereAPI.Proxy;

namespace ErabliereApi.Mcp.Models;

/// <summary>
/// Projection of a barrel of syrup returned to the MCP client.
/// </summary>
/// <param name="Id">Identifier of the barrel.</param>
/// <param name="IdErabliere">Identifier of the maple grove owning the barrel.</param>
/// <param name="Df">Date the barrel was closed.</param>
/// <param name="Qe">Estimated grade of the syrup, before classification.</param>
/// <param name="Q">Grade of the syrup after classification, null while unclassified.</param>
public record BarilSummary(
    [property: JsonPropertyName("id")] Guid? Id,
    [property: JsonPropertyName("idErabliere")] Guid? IdErabliere,
    [property: JsonPropertyName("df")] DateTimeOffset? Df,
    [property: JsonPropertyName("qe")] string? Qe,
    [property: JsonPropertyName("q")] string? Q)
{
    /// <summary>
    /// Maps a proxy DTO to the projection exposed by the MCP tools.
    /// </summary>
    public static BarilSummary From(Baril baril)
    {
        ArgumentNullException.ThrowIfNull(baril);

        return new BarilSummary(baril.Id, baril.IdErabliere, baril.Df, baril.Qe, baril.Q);
    }
}
