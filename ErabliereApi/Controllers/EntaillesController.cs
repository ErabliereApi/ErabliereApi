using ErabliereApi.Attributes;
using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Donnees.Action.Post;
using ErabliereApi.Donnees.Action.Put;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;

namespace ErabliereApi.Controllers;

/// <summary>
/// Contrôler représentant les entailles cartographiées d'une érablière
/// </summary>
[ApiController]
[Route("Erablieres/{id}/[controller]")]
[Authorize]
public class EntaillesController : ControllerBase
{
    private readonly ErabliereDbContext _depot;

    /// <summary>
    /// Constructeur par initialisation
    /// </summary>
    /// <param name="depot">Le dépôt des entailles</param>
    public EntaillesController(ErabliereDbContext depot)
    {
        _depot = depot;
    }

    /// <summary>
    /// Lister les entailles avec les fonctionnalités de OData
    /// </summary>
    /// <param name="id">Identifiant de l'érablière</param>
    /// <response code="200">Une liste d'entailles potentiellement vide.</response>
    [HttpGet]
    [EnableQuery]
    [ValiderOwnership("id")]
    public IQueryable<Entaille> Lister(Guid id)
    {
        return _depot.Entailles.AsNoTracking().Where(e => e.IdErabliere == id);
    }

    /// <summary>
    /// Ajouter une entaille
    /// </summary>
    /// <param name="id">L'identifiant de l'érablière</param>
    /// <param name="entaille">L'entaille à ajouter</param>
    /// <param name="token">Le token d'annulation</param>
    /// <response code="200">L'entaille a été correctement ajoutée.</response>
    /// <response code="400">La validation de l'entaille a échoué.</response>
    [HttpPost]
    [ValiderOwnership("id")]
    public async Task<IActionResult> Ajouter(Guid id, PostEntaille entaille, CancellationToken token)
    {
        if (id != entaille.IdErabliere)
        {
            return BadRequest("L'id de la route ne concorde pas avec l'id de l'entaille à ajouter.");
        }

        var erreur = await ValiderEntaille(id, entaille.Latitude, entaille.Longitude, entaille.IdArbre, entaille.IdLigneTubelure, token);

        if (erreur != null)
        {
            return BadRequest(erreur);
        }

        var entity = await _depot.Entailles.AddAsync(new Entaille
        {
            Id = entaille.Id,
            IdErabliere = entaille.IdErabliere,
            Nom = entaille.Nom,
            Latitude = entaille.Latitude,
            Longitude = entaille.Longitude,
            IdArbre = entaille.IdArbre,
            IdLigneTubelure = entaille.IdLigneTubelure,
            DC = DateTimeOffset.Now,
            DM = DateTimeOffset.Now
        }, token);

        await _depot.SaveChangesAsync(token);

        return Ok(new { id = entity.Entity.Id });
    }

    /// <summary>
    /// Modifier une entaille
    /// </summary>
    /// <param name="id">L'identifiant de l'érablière</param>
    /// <param name="entaille">L'entaille à modifier</param>
    /// <param name="token">Le token d'annulation</param>
    /// <response code="204">L'entaille a été correctement modifiée.</response>
    /// <response code="400">La validation de l'entaille a échoué.</response>
    [HttpPut]
    [ValiderOwnership("id")]
    public async Task<IActionResult> Modifier(Guid id, PutEntaille entaille, CancellationToken token)
    {
        if (id != entaille.IdErabliere)
        {
            return BadRequest("L'id de la route ne concorde pas avec l'id de l'entaille à modifier.");
        }

        if (entaille.Id == null)
        {
            return BadRequest("L'id de l'entaille à modifier est requis.");
        }

        var erreur = await ValiderEntaille(id, entaille.Latitude, entaille.Longitude, entaille.IdArbre, entaille.IdLigneTubelure, token);

        if (erreur != null)
        {
            return BadRequest(erreur);
        }

        var entity = await _depot.Entailles.FindAsync([entaille.Id], token);

        if (entity == null || entity.IdErabliere != id)
        {
            return NotFound();
        }

        entity.Nom = entaille.Nom;
        entity.Latitude = entaille.Latitude;
        entity.Longitude = entaille.Longitude;
        entity.IdArbre = entaille.IdArbre;
        entity.IdLigneTubelure = entaille.IdLigneTubelure;
        entity.DM = DateTimeOffset.Now;

        await _depot.SaveChangesAsync(token);

        return NoContent();
    }

    /// <summary>
    /// Supprimer une entaille
    /// </summary>
    /// <param name="id">Identifiant de l'érablière</param>
    /// <param name="idEntaille">Identifiant de l'entaille à supprimer</param>
    /// <param name="token">Le token d'annulation</param>
    /// <response code="204">L'entaille a été correctement supprimée.</response>
    /// <response code="404">L'entaille n'existe pas ou n'appartient pas à l'érablière.</response>
    [HttpDelete("{idEntaille}")]
    [ValiderOwnership("id")]
    public async Task<IActionResult> Supprimer(Guid id, Guid idEntaille, CancellationToken token)
    {
        var entaille = await _depot.Entailles.FindAsync([idEntaille], token);

        if (entaille == null || entaille.IdErabliere != id)
        {
            return NotFound();
        }

        _depot.Remove(entaille);

        await _depot.SaveChangesAsync(token);

        return NoContent();
    }

    /// <summary>
    /// Valider les coordonnées et les liens optionnels d'une entaille.
    /// L'arbre et la ligne de tubelure référencés doivent exister et appartenir à la même érablière.
    /// </summary>
    /// <returns>Un message d'erreur, ou null si l'entaille est valide</returns>
    private async Task<string?> ValiderEntaille(Guid id, double latitude, double longitude, Guid? idArbre, Guid? idLigneTubelure, CancellationToken token)
    {
        if (!ArbresController.CoordonneesValides(latitude, longitude))
        {
            return "L'entaille doit avoir une longitude entre -180 et 180 et une latitude entre -90 et 90.";
        }

        if (idArbre != null)
        {
            var arbre = await _depot.Arbres.FindAsync([idArbre], token);

            if (arbre == null || arbre.IdErabliere != id)
            {
                return "L'arbre référencé n'existe pas ou n'appartient pas à l'érablière.";
            }
        }

        if (idLigneTubelure != null)
        {
            var ligne = await _depot.LignesTubelure.FindAsync([idLigneTubelure], token);

            if (ligne == null || ligne.IdErabliere != id)
            {
                return "La ligne de tubelure référencée n'existe pas ou n'appartient pas à l'érablière.";
            }
        }

        return null;
    }
}
