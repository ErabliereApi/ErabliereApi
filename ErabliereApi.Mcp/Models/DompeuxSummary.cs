using System.Text.Json.Serialization;
using ErabliereAPI.Proxy;

namespace ErabliereApi.Mcp.Models;

/// <summary>
/// Projection of a dumping event ("dompeux"): one emptying cycle of the sap
/// collection tank.
/// </summary>
/// <param name="Id">Identifier of the event.</param>
/// <param name="T">Timestamp of the event.</param>
/// <param name="Dd">Start of the emptying cycle.</param>
/// <param name="Df">End of the emptying cycle.</param>
/// <param name="DureeSecondes">
/// Duration of the cycle in seconds, computed from <paramref name="Dd"/> and
/// <paramref name="Df"/>. Null when either bound is missing.
/// </param>
public record DompeuxSummary(
    [property: JsonPropertyName("id")] Guid? Id,
    [property: JsonPropertyName("t")] DateTimeOffset? T,
    [property: JsonPropertyName("dd")] DateTimeOffset? Dd,
    [property: JsonPropertyName("df")] DateTimeOffset? Df,
    [property: JsonPropertyName("dureeSecondes")] double? DureeSecondes)
{
    /// <summary>
    /// Maps a proxy DTO to the projection exposed by the MCP tools.
    /// </summary>
    public static DompeuxSummary From(GetDompeux dompeux)
    {
        ArgumentNullException.ThrowIfNull(dompeux);

        double? duree = dompeux.Dd.HasValue && dompeux.Df.HasValue
            ? Math.Round((dompeux.Df.Value - dompeux.Dd.Value).TotalSeconds, 1, MidpointRounding.AwayFromZero)
            : null;

        return new DompeuxSummary(dompeux.Id, dompeux.T, dompeux.Dd, dompeux.Df, duree);
    }
}
