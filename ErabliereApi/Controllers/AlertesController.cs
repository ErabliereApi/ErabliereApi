using ErabliereApi.Action;
using ErabliereApi.Action.Post;
using ErabliereApi.Attributes;
using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Donnees.Action.Put;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;

namespace ErabliereApi.Controllers;

/// <summary>
/// Contrôler pour interagir avec les alertes
/// </summary>
[ApiController]
[Route("Erablieres/{id}/[controller]")]
[Authorize]
public class AlertesController : ControllerBase
{
    private readonly ErabliereDbContext _depot;

    /// <summary>
    /// Constructeur par initialisation
    /// </summary>
    /// <param name="depot"></param>
    public AlertesController(ErabliereDbContext depot)
    {
        _depot = depot;
    }

    /// <summary>
    /// Liste les Alertes
    /// </summary>
    /// <param name="id">Identifiant de l'érablière</param>
    /// <param name="token">Jeton d'annulation de la tâche</param>
    /// <remarks>Les valeurs numérique sont en dixième de leurs unitées respective.</remarks>
    /// <response code="200">Une liste d'alerte potentiellement vide.</response>
    [HttpGet]
    [EnableQuery]
    [ValiderOwnership("id")]
    public async Task<IEnumerable<Alerte>> Lister(Guid id, CancellationToken token)
    {
        var alertes = await _depot.Alertes.AsNoTracking().Where(b => b.IdErabliere == id).ToArrayAsync(token);

        return alertes;
    }

    /// <summary>
    /// Ajouter une Alerte
    /// Séparer les adresse courriel par des ; pour saisir plusieurs adresses.
    /// </summary>
    /// <remarks>Chaque valeur numérique est en dixième. Donc pour représenter 1 degré celcius, il faut inscrire 10.</remarks>
    /// <param name="id">L'identifiant de l'érablière</param>
    /// <param name="postAlerte">Les paramètres de l'alerte</param>
    /// <param name="token">Jeton d'annulation de la tâche</param>
    /// <response code="200">L'alerte a été correctement ajouter.</response>
    /// <response code="400">L'id de la route ne concorde pas avec l'id de l'alerte à ajouter.</response>
    [HttpPost]
    [ValiderOwnership("id")]
    [ProducesDefaultResponseType(typeof(Alerte))]
    public async Task<IActionResult> Ajouter(Guid id, PostAlerte postAlerte, CancellationToken token)
    {
        if (id != postAlerte.IdErabliere)
        {
            return BadRequest("L'id de la route ne concorde pas avec l'id de l'alerte à ajouter");
        }

        var alerte = new Alerte
        {
            Id = postAlerte.Id,
            IdErabliere = postAlerte.IdErabliere,
            EnvoyerA = postAlerte.EnvoyerA,
            IsEnable = postAlerte.IsEnable,
            Nom = postAlerte.Nom,
            TexterA = postAlerte.TexterA,
            TemperatureThresholdHight = postAlerte.TemperatureThresholdHight,
            TemperatureThresholdLow = postAlerte.TemperatureThresholdLow,
            VacciumThresholdLow = postAlerte.VacciumThresholdLow,
            DC = DateTime.UtcNow
        };

        var entity = await _depot.Alertes.AddAsync(alerte, token);

        await _depot.SaveChangesAsync(token);

        return Ok(entity.Entity);
    }

    /// <summary>
    /// Modifier une alerte
    /// </summary>
    /// <param name="id">L'identifiant de l'érablière</param>
    /// <param name="alerte">L'alerte a modifier</param>
    /// <param name="token">Jeton d'annulation de la tâche</param>
    /// <response code="200">L'alerte a été correctement supprimé.</response>
    /// <response code="400">L'id de la route ne concorde pas avec l'id du baril à modifier.</response>
    [HttpPut]
    [ValiderOwnership("id")]
    public async Task<IActionResult> Modifier(Guid id, PutAlerte alerte, CancellationToken token)
    {
        if (id != alerte.IdErabliere)
        {
            return BadRequest("L'id de la route ne concorde pas avec l'id de l'alerte à modifier.");
        }
        if (alerte.Id == null)
        {
            return BadRequest("L'id de l'alerte à modifier est requis.");
        }

        var entity = await _depot.Alertes.FindAsync([alerte.Id], cancellationToken: token);

        if (entity == null || entity.IdErabliere != id)
        {
            return NotFound();
        }

        entity.Nom = alerte.Nom;
        entity.EnvoyerA = alerte.EnvoyerA;
        entity.TexterA = alerte.TexterA;
        entity.TemperatureThresholdLow = alerte.TemperatureThresholdLow;
        entity.TemperatureThresholdHight = alerte.TemperatureThresholdHight;
        entity.VacciumThresholdLow = alerte.VacciumThresholdLow;
        entity.VacciumThresholdHight = alerte.VacciumThresholdHight;
        entity.NiveauBassinThresholdLow = alerte.NiveauBassinThresholdLow;
        entity.NiveauBassinThresholdHight = alerte.NiveauBassinThresholdHight;
        entity.IsEnable = alerte.IsEnable;
        entity.DM = DateTimeOffset.UtcNow;

        await _depot.SaveChangesAsync(token);

        return Ok(entity);
    }

    /// <summary>
    /// Activer une alerte
    /// </summary>
    /// <param name="id">L'id de l'érablière</param>
    /// <param name="idAlerte">L'id de l'alerte</param>
    /// <param name="alerte">L'id de l'alerte</param>
    /// <param name="token">Le jeton d'annulation</param>
    /// <returns>Ok dans le cas d'un succès. NotFound avec l'alerte reçu qui n'a pas été trouvé.</returns>
    [HttpPut]
    [Route("{idAlerte}/[action]")]
    [ValiderOwnership("id")]
    public async Task<IActionResult> Activer(Guid id, Guid idAlerte, ErabliereOwnableArgs alerte, CancellationToken token)
    {
        if (id != alerte.IdErabliere)
        {
            return BadRequest("L'id de l'érablière dans la route ne concorde pas avec l'id de l'érablière de l'alerte à activer.");
        }
        if (idAlerte != alerte.Id)
        {
            return BadRequest("L'id de la route ne concorde pas avec l'id de l'alerte à activer.");
        }

        var entity = await _depot.Alertes.FindAsync([alerte.Id], cancellationToken: token);

        if (entity is not null && entity.IdErabliere == id)
        {
            entity.IsEnable = true;

            await _depot.SaveChangesAsync(token);

            return Ok();
        }

        return NotFound(alerte);
    }

    /// <summary>
    /// Désactiver une alerte
    /// </summary>
    /// <param name="id">L'id de l'érablière</param>
    /// <param name="idAlerte">L'id de l'alerte</param>
    /// <param name="alerte">L'id de l'alerte</param>
    /// <param name="token">Le jeton d'annulation</param>
    /// <returns>Ok dans le cas d'un succès. NotFound avec l'alerte reçu qui n'a pas été trouvé.</returns>
    [HttpPut]
    [Route("{idAlerte}/[action]")]
    [ValiderOwnership("id")]
    public async Task<IActionResult> Desactiver(Guid id, Guid idAlerte, ErabliereOwnableArgs alerte, CancellationToken token)
    {
        if (id != alerte.IdErabliere)
        {
            return BadRequest("L'id de l'érablière dans la route ne concorde pas avec l'id de l'érablière de l'alerte à désactiver.");
        }
        if (idAlerte != alerte.Id)
        {
            return BadRequest("L'id de la route ne concorde pas avec l'id de l'alerte à désactiver.");
        }

        var entity = await _depot.Alertes.FindAsync([alerte.Id], cancellationToken: token);

        if (entity is not null && entity.IdErabliere == id)
        {
            entity.IsEnable = false;

            await _depot.SaveChangesAsync(token);

            return Ok();
        }

        return NotFound(alerte);
    }

    /// <summary>
    /// Supprimer une alerte
    /// </summary>
    /// <param name="id">Identifiant de l'érablière</param>
    /// <param name="alerte">L'alerte à supprimer</param>
    /// <param name="token">Jeton d'annulation de la tâche</param>
    /// <response code="204">L'alerte a été correctement supprimé.</response>
    /// <response code="400">L'id de la route ne concorde pas avec l'id de l'alerte à supprimer.</response>
    [HttpDelete]
    [ValiderOwnership("id")]
    public async Task<IActionResult> Supprimer(Guid id, ErabliereOwnableArgs alerte, CancellationToken token)
    {
        if (id != alerte.IdErabliere)
        {
            return BadRequest("L'id de la route ne concorde pas avec l'id de l'alerte à supprimer.");
        }

        var alerteEntity = await _depot.Alertes.FindAsync([alerte.Id], cancellationToken: token);

        if (alerteEntity == null)
        {
            return NoContent();
        }

        if (id != alerteEntity.IdErabliere)
        {
            return BadRequest("L'id de la route ne concorde pas avec l'id de l'alerte à supprimer.");
        }

        _depot.Remove(alerteEntity);

        await _depot.SaveChangesAsync(token);

        return NoContent();
    }

    /// <summary>
    /// Supprimer une alerte
    /// </summary>
    /// <param name="id">Identifiant de l'érablière</param>
    /// <param name="idAlerte">L'id de l'alerte à supprimer</param>
    /// <param name="token">Jeton d'annulation de la tâche</param>
    /// <response code="204">L'alerte a été correctement supprimé.</response>
    /// <response code="400">L'id de la route ne concorde pas avec l'id de l'alerte à supprimer.</response>
    [HttpDelete("{idAlerte}")]
    [ValiderOwnership("id")]
    public async Task<IActionResult> Supprimer(Guid id, Guid idAlerte, CancellationToken token)
    {
        var alerte = await _depot.Alertes.FindAsync([idAlerte], cancellationToken: token);

        if (alerte == null)
        {
            return NoContent();
        }

        if (id != alerte.IdErabliere)
        {
            return BadRequest("L'id de la route ne concorde pas avec l'id de l'alerte à supprimer.");
        }

        _depot.Remove(alerte);

        await _depot.SaveChangesAsync(token);

        return NoContent();
    }
}
