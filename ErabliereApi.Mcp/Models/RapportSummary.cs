using System.Text.Json.Serialization;
using ErabliereAPI.Proxy;

namespace ErabliereApi.Mcp.Models;

/// <summary>
/// One row of a report, usually a day.
/// </summary>
/// <param name="Date">Date the row covers.</param>
/// <param name="Moyenne">Mean of the readings of the row.</param>
/// <param name="Somme">Sum of the readings of the row, the degree-days for a degree-day report.</param>
/// <param name="Min">Lowest reading of the row.</param>
/// <param name="Max">Highest reading of the row.</param>
public record RapportDonneeSummary(
    [property: JsonPropertyName("date")] DateTimeOffset? Date,
    [property: JsonPropertyName("moyenne")] double? Moyenne,
    [property: JsonPropertyName("somme")] double? Somme,
    [property: JsonPropertyName("min")] double? Min,
    [property: JsonPropertyName("max")] double? Max);

/// <summary>
/// Projection of a saved report. The rows are only present on the single report
/// tool; the listing tool leaves them out to stay cheap.
/// </summary>
/// <param name="Id">Identifier of the report.</param>
/// <param name="IdErabliere">Identifier of the maple grove owning the report.</param>
/// <param name="Nom">Display name of the report.</param>
/// <param name="Type">Kind of report, for instance a degree-day report.</param>
/// <param name="DateDebut">First day covered by the report.</param>
/// <param name="DateFin">Last day covered by the report.</param>
/// <param name="SeuilTemperature">Base temperature the degree-days are computed against.</param>
/// <param name="Moyenne">Mean over the whole report.</param>
/// <param name="Somme">Sum over the whole report.</param>
/// <param name="Min">Lowest value of the whole report.</param>
/// <param name="Max">Highest value of the whole report.</param>
/// <param name="IdCapteur">Identifier of the sensor the report was computed from, null when it used the weather data.</param>
/// <param name="CapteurNom">Name of that sensor, null when the report used the weather data.</param>
/// <param name="Dm">Date the report was last recomputed.</param>
/// <param name="Donnees">Rows of the report, null on the listing tool.</param>
public record RapportSummary(
    [property: JsonPropertyName("id")] Guid? Id,
    [property: JsonPropertyName("idErabliere")] Guid? IdErabliere,
    [property: JsonPropertyName("nom")] string? Nom,
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("dateDebut")] DateTimeOffset? DateDebut,
    [property: JsonPropertyName("dateFin")] DateTimeOffset? DateFin,
    [property: JsonPropertyName("seuilTemperature")] double? SeuilTemperature,
    [property: JsonPropertyName("moyenne")] double? Moyenne,
    [property: JsonPropertyName("somme")] double? Somme,
    [property: JsonPropertyName("min")] double? Min,
    [property: JsonPropertyName("max")] double? Max,
    [property: JsonPropertyName("idCapteur")] Guid? IdCapteur,
    [property: JsonPropertyName("capteurNom")] string? CapteurNom,
    [property: JsonPropertyName("dm")] DateTimeOffset? Dm,
    [property: JsonPropertyName("donnees")] IReadOnlyList<RapportDonneeSummary>? Donnees)
{
    /// <summary>
    /// Maps a proxy DTO to the projection exposed by the MCP tools.
    /// </summary>
    /// <param name="rapport">The report to project.</param>
    /// <param name="includeDonnees">
    /// True to include the rows. A season of daily rows is a few hundred entries,
    /// so the listing tool leaves them out.
    /// </param>
    public static RapportSummary From(Rapport rapport, bool includeDonnees)
    {
        ArgumentNullException.ThrowIfNull(rapport);

        var donnees = includeDonnees && rapport.Donnees is not null
            ? rapport.Donnees.OrderBy(donnee => donnee.Date)
                             .Select(donnee => new RapportDonneeSummary(donnee.Date, donnee.Moyenne, donnee.Somme, donnee.Min, donnee.Max))
                             .ToArray()
            : null;

        return new RapportSummary(
            rapport.Id,
            rapport.IdErabliere,
            rapport.Nom,
            rapport.Type,
            rapport.DateDebut,
            rapport.DateFin,
            rapport.SeuilTemperature,
            rapport.Moyenne,
            rapport.Somme,
            rapport.Min,
            rapport.Max,
            rapport.Capteur?.Id,
            rapport.Capteur?.Nom,
            rapport.Dm,
            donnees);
    }
}
