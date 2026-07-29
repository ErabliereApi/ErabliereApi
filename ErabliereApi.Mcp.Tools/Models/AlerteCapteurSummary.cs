using System.Text.Json.Serialization;
using ErabliereAPI.Proxy;

namespace ErabliereApi.Mcp.Models;

/// <summary>
/// Projection of a sensor alert returned to the MCP client.
/// Unlike <see cref="AlerteSummary"/>, which carries one threshold per kind of
/// measurement of the whole maple grove, a sensor alert watches a single sensor
/// and its bounds are expressed in the unit of that sensor.
/// </summary>
/// <param name="Id">Unique identifier of the alert.</param>
/// <param name="IdCapteur">Identifier of the watched sensor, the one to pass to get_donnees_capteur.</param>
/// <param name="CapteurNom">Name of the watched sensor, for instance "Température extérieure".</param>
/// <param name="CapteurSymbole">Unit symbol the thresholds are expressed in, for instance "°C" or "Hg".</param>
/// <param name="Nom">Display name of the alert.</param>
/// <param name="IsEnable">True when the alert is currently active.</param>
/// <param name="EnvoyerA">Email recipients, semicolon separated, may be null.</param>
/// <param name="TexterA">SMS recipients, semicolon separated, may be null.</param>
/// <param name="MinValue">Reading at or below which the alert fires, null when no lower bound is watched.</param>
/// <param name="MaxValue">Reading at or above which the alert fires, null when no upper bound is watched.</param>
/// <param name="LastOccurence">Last time the alert was triggered, null when it never triggered.</param>
public record AlerteCapteurSummary(
    [property: JsonPropertyName("id")] Guid? Id,
    [property: JsonPropertyName("idCapteur")] Guid? IdCapteur,
    [property: JsonPropertyName("capteurNom")] string? CapteurNom,
    [property: JsonPropertyName("capteurSymbole")] string? CapteurSymbole,
    [property: JsonPropertyName("nom")] string? Nom,
    [property: JsonPropertyName("isEnable")] bool? IsEnable,
    [property: JsonPropertyName("envoyerA")] string? EnvoyerA,
    [property: JsonPropertyName("texterA")] string? TexterA,
    [property: JsonPropertyName("minValue")] double? MinValue,
    [property: JsonPropertyName("maxValue")] double? MaxValue,
    [property: JsonPropertyName("lastOccurence")] DateTimeOffset? LastOccurence)
{
    /// <summary>
    /// Maps a proxy DTO to the projection exposed by the MCP tools. The name and
    /// the unit of the sensor are flattened out of the navigation property, which
    /// never reaches the model itself.
    /// </summary>
    public static AlerteCapteurSummary From(AlerteCapteur alerte)
    {
        ArgumentNullException.ThrowIfNull(alerte);

        return new AlerteCapteurSummary(
            alerte.Id,
            alerte.IdCapteur ?? alerte.Capteur?.Id,
            alerte.Capteur?.Nom,
            alerte.Capteur?.Symbole,
            alerte.Nom,
            alerte.IsEnable,
            alerte.EnvoyerA,
            alerte.TexterA,
            alerte.MinValue,
            alerte.MaxValue,
            alerte.LastOccurence);
    }
}
