using System.Text.Json.Serialization;
using ErabliereAPI.Proxy;

namespace ErabliereApi.Mcp.Models;

/// <summary>
/// Projection of a sensor returned to the MCP client.
/// </summary>
/// <param name="Id">Identifier of the sensor, the one to pass to get_donnees_capteur.</param>
/// <param name="IdErabliere">Identifier of the maple grove owning the sensor.</param>
/// <param name="Nom">Display name of the sensor, for instance "Température extérieure".</param>
/// <param name="Symbole">Unit symbol of the readings, for instance "°C" or "Hg". Null when the sensor has no unit.</param>
/// <param name="Type">Kind of sensor as configured by the producer, may be null.</param>
/// <param name="Online">True when the sensor was still reporting at the last check.</param>
/// <param name="LastMessageTime">Timestamp of the last message received from the sensor.</param>
/// <param name="BatteryLevel">Remaining battery in percent, null on a wired sensor.</param>
/// <param name="ReportFrequency">Expected delay between two readings, in seconds.</param>
public record CapteurSummary(
    [property: JsonPropertyName("id")] Guid? Id,
    [property: JsonPropertyName("idErabliere")] Guid? IdErabliere,
    [property: JsonPropertyName("nom")] string? Nom,
    [property: JsonPropertyName("symbole")] string? Symbole,
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("online")] bool? Online,
    [property: JsonPropertyName("lastMessageTime")] DateTimeOffset? LastMessageTime,
    [property: JsonPropertyName("batteryLevel")] int? BatteryLevel,
    [property: JsonPropertyName("reportFrequency")] int? ReportFrequency)
{
    /// <summary>
    /// Maps a proxy DTO to the projection exposed by the MCP tools.
    /// </summary>
    public static CapteurSummary From(Capteur capteur)
    {
        ArgumentNullException.ThrowIfNull(capteur);

        return new CapteurSummary(
            capteur.Id,
            capteur.IdErabliere,
            capteur.Nom,
            capteur.Symbole,
            capteur.Type,
            capteur.Online,
            capteur.LastMessageTime,
            capteur.BatteryLevel,
            capteur.ReportFrequency);
    }
}
