using System.Text.Json.Serialization;
using ErabliereAPI.Proxy;

namespace ErabliereApi.Mcp.Models;

/// <summary>
/// Projection of the weekly opening hours of a maple grove. Every day is a
/// "HH:mm-HH:mm" range, or null when the maple grove is closed that day.
/// </summary>
/// <param name="Id">Identifier of the schedule.</param>
/// <param name="IdErabliere">Identifier of the maple grove.</param>
/// <param name="Lundi">Monday opening range.</param>
/// <param name="Mardi">Tuesday opening range.</param>
/// <param name="Mercredi">Wednesday opening range.</param>
/// <param name="Jeudi">Thursday opening range.</param>
/// <param name="Vendredi">Friday opening range.</param>
/// <param name="Samedi">Saturday opening range.</param>
/// <param name="Dimanche">Sunday opening range.</param>
public record HoraireSummary(
    [property: JsonPropertyName("id")] Guid? Id,
    [property: JsonPropertyName("idErabliere")] Guid? IdErabliere,
    [property: JsonPropertyName("lundi")] string? Lundi,
    [property: JsonPropertyName("mardi")] string? Mardi,
    [property: JsonPropertyName("mercredi")] string? Mercredi,
    [property: JsonPropertyName("jeudi")] string? Jeudi,
    [property: JsonPropertyName("vendredi")] string? Vendredi,
    [property: JsonPropertyName("samedi")] string? Samedi,
    [property: JsonPropertyName("dimanche")] string? Dimanche)
{
    /// <summary>
    /// Maps a proxy DTO to the projection exposed by the MCP tools.
    /// </summary>
    public static HoraireSummary From(Horaire horaire)
    {
        ArgumentNullException.ThrowIfNull(horaire);

        return new HoraireSummary(
            horaire.Id,
            horaire.IdErabliere,
            horaire.Lundi,
            horaire.Mardi,
            horaire.Mercredi,
            horaire.Jeudi,
            horaire.Vendredi,
            horaire.Samedi,
            horaire.Dimanche);
    }
}
