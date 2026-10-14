using ErabliereApi.Attributes;
using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErabliereApi.Controllers;

/// <summary>
/// Contrôleur en lecture pour l'historique des épisodes d'alerte des capteurs,
/// par exemple les pannes de courant. Les épisodes sont créés et fermés par le serveur.
/// </summary>
[ApiController]
[Route("Capteurs/{id}/EpisodesAlerte")]
[Authorize]
public class EpisodesAlerteController : ControllerBase
{
    private readonly ErabliereDbContext _depot;

    /// <summary>
    /// Constructeur par initialisation
    /// </summary>
    public EpisodesAlerteController(ErabliereDbContext depot)
    {
        _depot = depot;
    }

    /// <summary>
    /// Liste les épisodes d'alerte d'un capteur, du plus récent au plus ancien
    /// </summary>
    /// <param name="id">Identifiant du capteur</param>
    /// <param name="debut">Début de la période. Un épisode commencé avant mais terminé après est inclus.</param>
    /// <param name="fin">Fin de la période. Un épisode commencé avant et terminé après est inclus.</param>
    /// <param name="token">Jeton d'annulation de la tâche</param>
    /// <response code="200">Une liste d'épisodes potentiellement vide. Un épisode en cours n'a pas de date de fin.</response>
    [HttpGet]
    [ValiderOwnership("id", typeof(Capteur))]
    public async Task<IEnumerable<EpisodeAlerteCapteur>> Lister(
        Guid id,
        [FromQuery] DateTimeOffset? debut,
        [FromQuery] DateTimeOffset? fin,
        CancellationToken token)
    {
        return await FiltrerPeriode(_depot.EpisodesAlerteCapteur.AsNoTracking().Where(e => e.IdCapteur == id), debut, fin)
            .OrderByDescending(e => e.DateDebut)
            .ToArrayAsync(token);
    }

    /// <summary>
    /// Liste les épisodes d'alerte de tous les capteurs d'une érablière, du plus récent au plus ancien
    /// </summary>
    /// <param name="id">Identifiant de l'érablière</param>
    /// <param name="debut">Début de la période. Un épisode commencé avant mais terminé après est inclus.</param>
    /// <param name="fin">Fin de la période. Un épisode commencé avant et terminé après est inclus.</param>
    /// <param name="token">Jeton d'annulation de la tâche</param>
    /// <response code="200">Une liste d'épisodes potentiellement vide. Un épisode en cours n'a pas de date de fin.</response>
    [HttpGet("/Erablieres/{id}/EpisodesAlerte")]
    [ValiderOwnership("id")]
    public async Task<IEnumerable<EpisodeAlerteCapteur>> ListerErabliere(
        Guid id,
        [FromQuery] DateTimeOffset? debut,
        [FromQuery] DateTimeOffset? fin,
        CancellationToken token)
    {
#nullable disable
        var query = _depot.EpisodesAlerteCapteur.AsNoTracking().Where(e => e.Capteur.IdErabliere == id);
#nullable enable

        return await FiltrerPeriode(query, debut, fin)
            .OrderByDescending(e => e.DateDebut)
            .ToArrayAsync(token);
    }

    /// <summary>
    /// Garde les épisodes qui chevauchent la période. Un épisode en cours s'étend jusqu'à maintenant.
    /// </summary>
    private static IQueryable<EpisodeAlerteCapteur> FiltrerPeriode(IQueryable<EpisodeAlerteCapteur> query, DateTimeOffset? debut, DateTimeOffset? fin)
    {
        if (debut.HasValue)
        {
            query = query.Where(e => e.DateFin == null || e.DateFin >= debut.Value);
        }

        if (fin.HasValue)
        {
            query = query.Where(e => e.DateDebut <= fin.Value);
        }

        return query;
    }
}
