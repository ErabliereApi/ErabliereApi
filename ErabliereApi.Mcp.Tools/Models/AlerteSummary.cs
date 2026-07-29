using System.Text.Json.Serialization;
using ErabliereAPI.Proxy;

namespace ErabliereApi.Mcp.Models;

/// <summary>
/// Projection of an alert returned to the MCP client.
/// The thresholds are stored as strings by the API, they are forwarded as-is.
/// </summary>
/// <param name="Id">Unique identifier of the alert.</param>
/// <param name="IdErabliere">Identifier of the maple grove owning the alert.</param>
/// <param name="Nom">Display name of the alert.</param>
/// <param name="IsEnable">True when the alert is currently active.</param>
/// <param name="EnvoyerA">Email recipients, semicolon separated, may be null.</param>
/// <param name="TexterA">SMS recipients, semicolon separated, may be null.</param>
/// <param name="TemperatureThresholdLow">Low temperature threshold triggering the alert.</param>
/// <param name="TemperatureThresholdHight">High temperature threshold triggering the alert.</param>
/// <param name="VacciumThresholdLow">Low vacuum threshold triggering the alert.</param>
/// <param name="VacciumThresholdHight">High vacuum threshold triggering the alert.</param>
/// <param name="NiveauBassinThresholdLow">Low tank level threshold triggering the alert.</param>
/// <param name="NiveauBassinThresholdHight">High tank level threshold triggering the alert.</param>
/// <param name="LastOccurence">Last time the alert was triggered, null when it never triggered.</param>
public record AlerteSummary(
    [property: JsonPropertyName("id")] Guid? Id,
    [property: JsonPropertyName("idErabliere")] Guid? IdErabliere,
    [property: JsonPropertyName("nom")] string? Nom,
    [property: JsonPropertyName("isEnable")] bool? IsEnable,
    [property: JsonPropertyName("envoyerA")] string? EnvoyerA,
    [property: JsonPropertyName("texterA")] string? TexterA,
    [property: JsonPropertyName("temperatureThresholdLow")] string? TemperatureThresholdLow,
    [property: JsonPropertyName("temperatureThresholdHight")] string? TemperatureThresholdHight,
    [property: JsonPropertyName("vacciumThresholdLow")] string? VacciumThresholdLow,
    [property: JsonPropertyName("vacciumThresholdHight")] string? VacciumThresholdHight,
    [property: JsonPropertyName("niveauBassinThresholdLow")] string? NiveauBassinThresholdLow,
    [property: JsonPropertyName("niveauBassinThresholdHight")] string? NiveauBassinThresholdHight,
    [property: JsonPropertyName("lastOccurence")] DateTimeOffset? LastOccurence)
{
    /// <summary>
    /// Maps a proxy DTO to the projection exposed by the MCP tools.
    /// </summary>
    public static AlerteSummary From(Alerte alerte)
    {
        ArgumentNullException.ThrowIfNull(alerte);

        return new AlerteSummary(
            alerte.Id,
            alerte.IdErabliere,
            alerte.Nom,
            alerte.IsEnable,
            alerte.EnvoyerA,
            alerte.TexterA,
            alerte.TemperatureThresholdLow,
            alerte.TemperatureThresholdHight,
            alerte.VacciumThresholdLow,
            alerte.VacciumThresholdHight,
            alerte.NiveauBassinThresholdLow,
            alerte.NiveauBassinThresholdHight,
            alerte.LastOccurence);
    }
}
