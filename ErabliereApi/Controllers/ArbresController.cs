using ErabliereApi.Attributes;
using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;

namespace ErabliereApi.Controllers;

/// <summary>
/// Contrôler représentant les arbres cartographiés d'une érablière
/// </summary>
[ApiController]
[Route("Erablieres/{id}/[controller]")]
[Authorize]
public class ArbresController : ControllerBase
{
    private readonly ErabliereDbContext _depot;

    /// <summary>
    /// Constructeur par initialisation
    /// </summary>
    /// <param name="depot">Le dépôt des arbres</param>
    public ArbresController(ErabliereDbContext depot)
    {
        _depot = depot;
    }

    /// <summary>
    /// Lister les arbres avec les fonctionnalités de OData
    /// </summary>
    /// <param name="id">Identifiant de l'érablière</param>
    /// <response code="200">Une liste d'arbres potentiellement vide.</response>
    [HttpGet]
    [EnableQuery]
    [ValiderOwnership("id")]
    public IQueryable<Arbre> Lister(Guid id)
    {
        return _depot.Arbres.AsNoTracking().Where(a => a.IdErabliere == id);
    }

    /// <summary>
    /// Ajouter un arbre
    /// </summary>
    /// <param name="id">L'identifiant de l'érablière</param>
    /// <param name="arbre">L'arbre à ajouter</param>
    /// <param name="token">Le token d'annulation</param>
    /// <response code="200">L'arbre a été correctement ajouté.</response>
    /// <response code="400">La validation de l'arbre a échoué.</response>
    [HttpPost]
    [ValiderOwnership("id")]
    public async Task<IActionResult> Ajouter(Guid id, Arbre arbre, CancellationToken token)
    {
        if (id != arbre.IdErabliere)
        {
            return BadRequest("L'id de la route ne concorde pas avec l'id de l'arbre à ajouter.");
        }

        if (!CoordonneesValides(arbre.Latitude, arbre.Longitude))
        {
            return BadRequest("L'arbre doit avoir une longitude entre -180 et 180 et une latitude entre -90 et 90.");
        }

        arbre.DC = DateTimeOffset.Now;
        arbre.DM = DateTimeOffset.Now;

        var entity = await _depot.Arbres.AddAsync(arbre, token);

        await _depot.SaveChangesAsync(token);

        return Ok(new { id = entity.Entity.Id });
    }

    /// <summary>
    /// Modifier un arbre
    /// </summary>
    /// <param name="id">L'identifiant de l'érablière</param>
    /// <param name="arbre">L'arbre à modifier</param>
    /// <param name="token">Le token d'annulation</param>
    /// <response code="204">L'arbre a été correctement modifié.</response>
    /// <response code="400">La validation de l'arbre a échoué.</response>
    [HttpPut]
    [ValiderOwnership("id")]
    public async Task<IActionResult> Modifier(Guid id, Arbre arbre, CancellationToken token)
    {
        if (id != arbre.IdErabliere)
        {
            return BadRequest("L'id de la route ne concorde pas avec l'id de l'arbre à modifier.");
        }

        if (!CoordonneesValides(arbre.Latitude, arbre.Longitude))
        {
            return BadRequest("L'arbre doit avoir une longitude entre -180 et 180 et une latitude entre -90 et 90.");
        }

        arbre.DM = DateTimeOffset.Now;

        _depot.Update(arbre);

        await _depot.SaveChangesAsync(token);

        return NoContent();
    }

    /// <summary>
    /// Supprimer un arbre. Les entailles rattachées à l'arbre sont conservées,
    /// mais leur lien vers l'arbre est retiré.
    /// </summary>
    /// <param name="id">Identifiant de l'érablière</param>
    /// <param name="idArbre">Identifiant de l'arbre à supprimer</param>
    /// <param name="token">Le token d'annulation</param>
    /// <response code="204">L'arbre a été correctement supprimé.</response>
    /// <response code="404">L'arbre n'existe pas ou n'appartient pas à l'érablière.</response>
    [HttpDelete("{idArbre}")]
    [ValiderOwnership("id")]
    public async Task<IActionResult> Supprimer(Guid id, Guid idArbre, CancellationToken token)
    {
        var arbre = await _depot.Arbres.FindAsync([idArbre], token);

        if (arbre == null || arbre.IdErabliere != id)
        {
            return NotFound();
        }

        var entailles = await _depot.Entailles
            .Where(e => e.IdArbre == idArbre)
            .ToListAsync(token);

        foreach (var entaille in entailles)
        {
            entaille.IdArbre = null;
        }

        _depot.Remove(arbre);

        await _depot.SaveChangesAsync(token);

        return NoContent();
    }

    /// <summary>
    /// Valider qu'une paire latitude/longitude est dans les bornes valides
    /// </summary>
    internal static bool CoordonneesValides(double latitude, double longitude)
    {
        return latitude >= -90 && latitude <= 90 && longitude >= -180 && longitude <= 180;
    }
}
