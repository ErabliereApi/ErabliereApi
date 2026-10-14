using ErabliereApi.Attributes;
using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Donnees.Action.Delete;
using ErabliereApi.Donnees.Action.Post;
using ErabliereApi.Donnees.Action.Put;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErabliereApi.Controllers;

/// <summary>
/// Contrôler pour interagir avec les alertes des capteurs
/// </summary>
[ApiController]
[Route("Capteurs/{id}/[controller]")]
[Authorize]
public class AlerteCapteursController : ControllerBase
{
    private readonly ErabliereDbContext _depot;

    /// <summary>
    /// Constructeur par initialisation
    /// </summary>
    public AlerteCapteursController(ErabliereDbContext depot)
    {
        _depot = depot;
    }

    /// <summary>
    /// Liste les alertes d'un capteur
    /// </summary>
    /// <param name="id">Identifiant du capteur</param>
    /// <param name="token">Jeton d'annulation de la tâche</param>
    /// <remarks>Les valeurs numérique sont en dixième de leurs unitées respective.</remarks>
    /// <response code="200">Une liste d'alerte potentiellement vide.</response>
    [HttpGet]
    [ValiderOwnership("id", typeof(Capteur))]
    public async Task<IEnumerable<AlerteCapteur>> Lister(Guid id, CancellationToken token)
    {
        var alertes = await _depot.AlerteCapteurs.AsNoTracking().Where(b => b.IdCapteur == id).ToArrayAsync(token);

        return alertes;
    }

    /// <summary>
    /// Lister les alertes des capteurs d'une erablière
    /// </summary>
    /// <param name="id">L'id de l'érablière</param>
    /// <param name="include">Inclure les propriétés de navigation</param>
    /// <param name="token">Jeton d'annulation de la tâche</param>
    /// <returns></returns>
    [Route("/Erablieres/{id}/AlertesCapteur")]
    [HttpGet]
    [ValiderOwnership("id")]
    public async Task<IEnumerable<AlerteCapteur>> ListerAlerteCapteurErabliere(
        Guid id,
        [FromQuery] string? include,
        CancellationToken token)
    {
        var alertesCapteursQuery = _depot.AlerteCapteurs
            .AsNoTracking()
#nullable disable
            .Where(b => b.Capteur.IdErabliere == id);
#nullable enable

        if (include != null)
        {
            foreach (var property in include.Split(','))
            {
                alertesCapteursQuery = alertesCapteursQuery.Include(property);
            }
        }

        var alertesCapteurs = await alertesCapteursQuery.ToArrayAsync(token);

        return alertesCapteurs;
    }

    /// <summary>
    /// Ajouter une Alerte.
    /// Séparer les adresse courriel par des ; pour saisir plusieurs adresses.
    /// </summary>
    /// <remarks>Chaque valeur numérique est en dixième. Donc pour représenter 1 degré celcius, il faut inscrire 10.</remarks>
    /// <param name="id">L'identifiant de l'érablière</param>
    /// <param name="alerte">Les paramètres de l'alerte</param>
    /// <param name="token">Jeton d'annulation de la tâche</param>
    /// <response code="200">L'alerte a été correctement ajouter.</response>
    /// <response code="400">L'id de la route ne concorde pas avec l'id de l'alerte à ajouter.</response>
    /// <response code="400">Le capteur n'existe pas</response>
    [HttpPost]
    [ValiderOwnership("id", typeof(Capteur))]
    public async Task<IActionResult> Ajouter(Guid id, PostAlerteCapteur alerte, CancellationToken token)
    {
        if (id != alerte.IdCapteur)
        {
            return BadRequest("L'id de la route ne concorde pas avec l'id du capteur à ajouter");
        }

        if (await (_depot.Capteurs.FindAsync([ id ], cancellationToken: token)) == null)
        {
            return BadRequest("Le capteur n'existe pas");
        }

        var entity = await _depot.AlerteCapteurs.AddAsync(new AlerteCapteur
        {
            Id = alerte.Id,
            IdCapteur = alerte.IdCapteur,
            Nom = alerte.Nom,
            EnvoyerA = alerte.EnvoyerA,
            TexterA = alerte.TexterA,
            MinValue = alerte.MinValue,
            MaxValue = alerte.MaxValue,
            Marge = alerte.Marge,
            IsEnable = alerte.IsEnable,
            DC = alerte.DC ?? DateTime.Now
        }, token);

        await _depot.SaveChangesAsync(token);

        return Ok(entity.Entity);
    }

    /// <summary>
    /// Modifier une alerte
    /// </summary>
    /// <param name="id">L'identifiant du capteur</param>
    /// <param name="alerte">L'alerte a modifier</param>
    /// <param name="token">Jeton d'annulation de la tâche</param>
    /// <response code="200">L'alerte a été correctement supprimé.</response>
    /// <response code="400">L'id de la route ne concorde pas avec l'id du baril à modifier.</response>
    [HttpPut]
    [ValiderOwnership("id", typeof(Capteur))]
    public async Task<IActionResult> Modifier(Guid id, PutAlerteCapteur alerte, CancellationToken token)
    {
        if (id != alerte.IdCapteur)
        {
            return BadRequest("L'id de la route ne concorde pas avec l'id de l'alerte à modifier.");
        }

        var alerteCapteur = await _depot.AlerteCapteurs.Include(a => a.Capteur).FirstOrDefaultAsync(a => a.Id == alerte.Id && a.IdCapteur == id, token);

        if (alerteCapteur == null)
        {
            return NotFound("L'alerte de capteur n'existe pas");
        }

        if (alerteCapteur.IsEnable && !alerte.IsEnable)
        {
            await ReinitialiserEtatAsync(alerteCapteur, RaisonFinEpisode.AlerteDesactivee, token);
        }
        else if (alerteCapteur.MinValue != alerte.MinValue ||
                 alerteCapteur.MaxValue != alerte.MaxValue ||
                 alerteCapteur.Marge != alerte.Marge)
        {
            await ReinitialiserEtatAsync(alerteCapteur, RaisonFinEpisode.AlerteModifiee, token);
        }

        alerteCapteur.IsEnable = alerte.IsEnable;
        alerteCapteur.EnvoyerA = alerte.EnvoyerA;
        alerteCapteur.TexterA = alerte.TexterA;
        alerteCapteur.MaxValue = alerte.MaxValue;
        alerteCapteur.MinValue = alerte.MinValue;
        alerteCapteur.Marge = alerte.Marge;
        alerteCapteur.Nom = alerte.Nom;
        alerteCapteur.DM = DateTimeOffset.UtcNow;

        var entity = _depot.Update(alerteCapteur);

        await _depot.SaveChangesAsync(token);

        return Ok(entity.Entity);
    }

    /// <summary>
    /// Activer une alerte capteur
    /// </summary>
    /// <param name="id">L'identifiant du capteur</param>
    /// <param name="idAlerte">L'id de l'alerte</param>
    /// <param name="alerte">L'alerte a modifier</param>
    /// <param name="token">Jeton d'annulation de la tâche</param>
    /// <response code="200">L'alerte a été correctement supprimé.</response>
    /// <response code="400">L'id de la route ne concorde pas avec l'id du baril à modifier.</response>
    [HttpPut]
    [Route("{idAlerte}/[action]")]
    [ValiderOwnership("id", typeof(Capteur))]
    public async Task<IActionResult> Activer(Guid id, Guid idAlerte, PutAlerteCapteur alerte, CancellationToken token)
    {
        if (id != alerte.IdCapteur)
        {
            return BadRequest("L'id du capteur de la route ne concorde pas avec l'id du capteur de l'alerte à activer.");
        }
        if (idAlerte != alerte.Id)
        {
            return BadRequest("L'id de l'alerte ne concorde pas avec l'id de l'alerte à modifier.");
        }

        var entity = await _depot.AlerteCapteurs.FindAsync([ alerte.Id ], cancellationToken: token);

        if (entity is not null && entity.IdCapteur == id)
        {
            entity.IsEnable = true;

            await _depot.SaveChangesAsync(token);

            return Ok();
        }

        return NotFound();
    }

    /// <summary>
    /// Activer une alerte capteur
    /// </summary>
    /// <param name="id">L'identifiant du capteur</param>
    /// <param name="idAlerte">L'id de l'alerte</param>
    /// <param name="alerte">L'alerte a modifier</param>
    /// <param name="token">Jeton d'annulation de la tâche</param>
    /// <response code="200">L'alerte a été correctement supprimé.</response>
    /// <response code="400">L'id de la route ne concorde pas avec l'id du baril à modifier.</response>
    [HttpPut]
    [Route("{idAlerte}/[action]")]
    [ValiderOwnership("id", typeof(Capteur))]
    public async Task<IActionResult> Desactiver(Guid id, Guid idAlerte, PutAlerteCapteur alerte, CancellationToken token)
    {
        if (id != alerte.IdCapteur)
        {
            return BadRequest("L'id du capteur de la route ne concorde pas avec l'id du capteur de l'alerte à désactiver.");
        }
        if (idAlerte != alerte.Id)
        {
            return BadRequest("L'id de l'alerte de la route ne concorde pas avec l'id de l'alerte à désactiver.");
        }

        var entity = await _depot.AlerteCapteurs.FindAsync(new object?[] { idAlerte }, cancellationToken: token);

        if (entity is not null && entity.IdCapteur == id)
        {
            entity.IsEnable = false;
            await ReinitialiserEtatAsync(entity, RaisonFinEpisode.AlerteDesactivee, token);

            await _depot.SaveChangesAsync(token);

            return Ok();
        }

        return NotFound();
    }

    /// <summary>
    /// Remet l'alerte à l'état normal sans envoyer de message. Les seuils ayant changé ou l'alerte
    /// ayant été désactivée, l'état sera réévalué à la prochaine donnée. La date empêche une
    /// donnée envoyée en différé de relancer l'alerte sur la base de l'ancienne configuration.
    /// L'épisode en cours est fermé à cette date, sans valeur : sa fin n'a pas été mesurée.
    /// </summary>
    private async Task ReinitialiserEtatAsync(AlerteCapteur alerte, RaisonFinEpisode raison, CancellationToken token)
    {
        var maintenant = DateTimeOffset.Now;

        alerte.EstEnAlerte = false;
        alerte.DateChangementEtat = maintenant;

        await FermerEpisodesSansMesureAsync(alerte.Id, maintenant, raison, token);
    }

    /// <summary>
    /// Ferme les épisodes en cours d'une alerte à une date qui n'est pas celle d'une donnée.
    /// </summary>
    private async Task FermerEpisodesSansMesureAsync(Guid? idAlerte, DateTimeOffset date, RaisonFinEpisode raison, CancellationToken token)
    {
        var episodes = await _depot.EpisodesAlerteCapteur
            .Where(e => e.IdAlerteCapteur == idAlerte && e.DateFin == null)
            .ToArrayAsync(token);

        foreach (var episode in episodes)
        {
            episode.DateFin = date;
            episode.ValeurFin = null;
            episode.RaisonFin = raison;
        }
    }

    /// <summary>
    /// Ferme les épisodes en cours de l'alerte et les détache d'elle : l'historique des épisodes
    /// survit à la suppression de l'alerte.
    /// </summary>
    private async Task DetacherEpisodesAsync(Guid? idAlerte, CancellationToken token)
    {
        var episodes = await _depot.EpisodesAlerteCapteur
            .Where(e => e.IdAlerteCapteur == idAlerte)
            .ToArrayAsync(token);

        var maintenant = DateTimeOffset.Now;

        foreach (var episode in episodes)
        {
            if (episode.DateFin == null)
            {
                episode.DateFin = maintenant;
                episode.ValeurFin = null;
                episode.RaisonFin = RaisonFinEpisode.AlerteSupprimee;
            }

            episode.IdAlerteCapteur = null;
        }
    }

    /// <summary>
    /// Supprimer une alerte
    /// </summary>
    /// <param name="id">Identifiant du capteur</param>
    /// <param name="alerte">L'alerte à supprimer</param>
    /// <param name="token">Jeton d'annulation de la tâche</param>
    /// <response code="204">L'alerte a été correctement supprimé.</response>
    /// <response code="400">L'id de la route ne concorde pas avec l'id de l'alerte à supprimer.</response>
    /// <response code="404">L'alerte n'existe pas sur ce capteur.</response>
    [HttpDelete]
    [ValiderOwnership("id", typeof(Capteur))]
    public async Task<IActionResult> Supprimer(Guid id, DeleteAlerteCapteur alerte, CancellationToken token)
    {
        if (id != alerte.IdCapteur)
        {
            return BadRequest("L'id de la route ne concorde pas avec l'id de l'alerte à supprimer.");
        }

        // ValiderOwnership ne protège que le capteur de la route : l'alerte est chargée par le
        // serveur et doit lui appartenir, sinon l'id du corps viserait l'alerte d'un autre client.
        var entity = await _depot.AlerteCapteurs.FirstOrDefaultAsync(a => a.Id == alerte.Id && a.IdCapteur == id, token);

        if (entity == null)
        {
            return NotFound();
        }

        await DetacherEpisodesAsync(entity.Id, token);

        _depot.Remove(entity);

        await _depot.SaveChangesAsync(token);

        return NoContent();
    }

    /// <summary>
    /// Supprimer une alerte
    /// </summary>
    /// <param name="id">Identifiant du capteur</param>
    /// <param name="idAlerte">L'id de l'alerte à supprimer</param>
    /// <param name="token">Jeton d'annulation de la tâche</param>
    /// <response code="204">L'alerte a été correctement supprimé.</response>
    /// <response code="400">L'id de la route ne concorde pas avec l'id de l'alerte à supprimer.</response>
    [HttpDelete("{idAlerte}")]
    [ValiderOwnership("id", typeof(Capteur))]
    public async Task<IActionResult> Supprimer(Guid id, Guid idAlerte, CancellationToken token)
    {
        var alerte = await _depot.AlerteCapteurs.FindAsync(new object?[] { idAlerte }, cancellationToken: token);

        if (alerte == null)
        {
            return NoContent();
        }

        if (id != alerte.IdCapteur)
        {
            return BadRequest("L'id de la route ne concorde pas avec l'id de l'alerte à supprimer.");
        }

        await DetacherEpisodesAsync(alerte.Id, token);

        _depot.Remove(alerte);

        await _depot.SaveChangesAsync(token);

        return NoContent();
    }
}
