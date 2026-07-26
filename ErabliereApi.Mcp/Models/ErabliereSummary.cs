using System.Text.Json.Serialization;
using ErabliereAPI.Proxy;

namespace ErabliereApi.Mcp.Models;

/// <summary>
/// Projection of a maple grove returned to the MCP client.
/// The proxy DTO carries a dozen navigation collections that are always null on
/// these endpoints, so only the meaningful scalar fields are exposed here.
/// </summary>
/// <param name="Id">Unique identifier of the maple grove.</param>
/// <param name="Nom">Display name of the maple grove.</param>
/// <param name="Description">Free text description, may be null.</param>
/// <param name="Addresse">Civic address, may be null.</param>
/// <param name="CodePostal">Postal code, may be null.</param>
/// <param name="RegionAdministrative">Administrative region, may be null.</param>
/// <param name="IsPublic">True when the maple grove data is publicly readable.</param>
/// <param name="IndiceOrdre">Display order used by the web application.</param>
public record ErabliereSummary(
    [property: JsonPropertyName("id")] Guid? Id,
    [property: JsonPropertyName("nom")] string? Nom,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("addresse")] string? Addresse,
    [property: JsonPropertyName("codePostal")] string? CodePostal,
    [property: JsonPropertyName("regionAdministrative")] string? RegionAdministrative,
    [property: JsonPropertyName("isPublic")] bool? IsPublic,
    [property: JsonPropertyName("indiceOrdre")] int? IndiceOrdre)
{
    /// <summary>
    /// Maps a proxy DTO to the projection exposed by the MCP tools.
    /// </summary>
    public static ErabliereSummary From(Erabliere erabliere)
    {
        ArgumentNullException.ThrowIfNull(erabliere);

        return new ErabliereSummary(
            erabliere.Id,
            erabliere.Nom,
            erabliere.Description,
            erabliere.Addresse,
            erabliere.CodePostal,
            erabliere.RegionAdministrative,
            erabliere.IsPublic,
            erabliere.IndiceOrdre);
    }
}
