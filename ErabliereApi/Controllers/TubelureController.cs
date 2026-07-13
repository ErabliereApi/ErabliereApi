using ErabliereApi.Attributes;
using ErabliereApi.Depot.Sql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ErabliereApi.Controllers;

/// <summary>
/// Contrôler permettant d'obtenir le réseau de tubelure complet d'une érablière
/// (lignes, arbres et entailles) sous format GeoJson
/// </summary>
[ApiController]
[Route("Erablieres/{id}/[controller]")]
[Authorize]
public class TubelureController : ControllerBase
{
    private readonly ErabliereDbContext _depot;
    private readonly ILogger<TubelureController> _logger;

    /// <summary>
    /// Constructeur par initialisation
    /// </summary>
    /// <param name="depot">Le dépôt des données</param>
    /// <param name="logger">Le logger</param>
    public TubelureController(ErabliereDbContext depot, ILogger<TubelureController> logger)
    {
        _depot = depot;
        _logger = logger;
    }

    /// <summary>
    /// Obtenir le réseau de tubelure de l'érablière sous format GeoJson.
    /// Les lignes sont des features LineString, les arbres et les entailles des features Point.
    /// </summary>
    /// <param name="id">Identifiant de l'érablière</param>
    /// <param name="token">Le token d'annulation</param>
    /// <response code="200">Une FeatureCollection GeoJson.</response>
    [HttpGet("GeoJson")]
    [ValiderOwnership("id")]
    public async Task<IActionResult> GetGeoJson(Guid id, CancellationToken token)
    {
        var lignes = await _depot.LignesTubelure.AsNoTracking()
            .Where(l => l.IdErabliere == id)
            .ToArrayAsync(token);

        var arbres = await _depot.Arbres.AsNoTracking()
            .Where(a => a.IdErabliere == id)
            .ToArrayAsync(token);

        var entailles = await _depot.Entailles.AsNoTracking()
            .Where(e => e.IdErabliere == id)
            .ToArrayAsync(token);

        var features = new List<object>();

        foreach (var ligne in lignes)
        {
            double[][]? coordonnees = null;

            try
            {
                if (!string.IsNullOrWhiteSpace(ligne.CoordonneesJson))
                {
                    coordonnees = JsonSerializer.Deserialize<double[][]>(ligne.CoordonneesJson);
                }
            }
            catch (JsonException e)
            {
                _logger.LogWarning(e, "Les coordonnées de la ligne de tubelure {IdLigne} sont invalides, la ligne est ignorée du GeoJson.", ligne.Id);
            }

            if (coordonnees == null || coordonnees.Length < 2)
            {
                continue;
            }

            features.Add(new
            {
                type = "Feature",
                geometry = new
                {
                    type = "LineString",
                    coordinates = coordonnees
                },
                properties = new
                {
                    id = ligne.Id,
                    nom = ligne.Nom,
                    typeLigne = ligne.TypeLigne,
                    feature = "ligne"
                }
            });
        }

        features.AddRange(arbres.Select(arbre => (object)new
        {
            type = "Feature",
            geometry = new
            {
                type = "Point",
                coordinates = new[] { arbre.Longitude, arbre.Latitude }
            },
            properties = new
            {
                id = arbre.Id,
                nom = arbre.Nom,
                espece = arbre.Espece,
                feature = "arbre"
            }
        }));

        features.AddRange(entailles.Select(entaille => (object)new
        {
            type = "Feature",
            geometry = new
            {
                type = "Point",
                coordinates = new[] { entaille.Longitude, entaille.Latitude }
            },
            properties = new
            {
                id = entaille.Id,
                nom = entaille.Nom,
                idArbre = entaille.IdArbre,
                idLigneTubelure = entaille.IdLigneTubelure,
                feature = "entaille"
            }
        }));

        var geoJson = new
        {
            type = "FeatureCollection",
            features = features.ToArray()
        };

        return Ok(geoJson);
    }
}
