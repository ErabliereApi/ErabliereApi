using ErabliereApi.Attributes;
using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Donnees.Action.Post;
using ErabliereApi.Donnees.Action.Put;
using ErabliereApi.Donnees.Contantes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ErabliereApi.Controllers;

/// <summary>
/// Contrôler représentant les lignes du réseau de tubelure
/// </summary>
[ApiController]
[Route("Erablieres/{id}/[controller]")]
[Authorize]
public class LignesTubelureController : ControllerBase
{
    private readonly ErabliereDbContext _depot;

    /// <summary>
    /// Nombre maximal de points d'une ligne de tubelure
    /// </summary>
    public const int MaxPoints = 10000;

    /// <summary>
    /// Constructeur par initialisation
    /// </summary>
    /// <param name="depot">Le dépôt des lignes de tubelure</param>
    public LignesTubelureController(ErabliereDbContext depot)
    {
        _depot = depot;
    }

    /// <summary>
    /// Lister les lignes de tubelure avec les fonctionnalités de OData
    /// </summary>
    /// <param name="id">Identifiant de l'érablière</param>
    /// <response code="200">Une liste de lignes de tubelure potentiellement vide.</response>
    [HttpGet]
    [EnableQuery]
    [ValiderOwnership("id")]
    public IQueryable<LigneTubelure> Lister(Guid id)
    {
        return _depot.LignesTubelure.AsNoTracking().Where(l => l.IdErabliere == id);
    }

    /// <summary>
    /// Ajouter une ligne de tubelure
    /// </summary>
    /// <param name="id">L'identifiant de l'érablière</param>
    /// <param name="ligne">La ligne à ajouter</param>
    /// <param name="token">Le token d'annulation</param>
    /// <response code="200">La ligne a été correctement ajoutée.</response>
    /// <response code="400">La validation de la ligne a échoué.</response>
    [HttpPost]
    [ValiderOwnership("id")]
    public async Task<IActionResult> Ajouter(Guid id, PostLigneTubelure ligne, CancellationToken token)
    {
        if (id != ligne.IdErabliere)
        {
            return BadRequest("L'id de la route ne concorde pas avec l'id de la ligne à ajouter.");
        }

        var erreur = ValiderLigne(ligne.TypeLigne, ligne.CoordonneesJson);

        if (erreur != null)
        {
            return BadRequest(erreur);
        }

        var entity = await _depot.LignesTubelure.AddAsync(new LigneTubelure
        {
            Id = ligne.Id,
            IdErabliere = ligne.IdErabliere,
            Nom = ligne.Nom,
            TypeLigne = ligne.TypeLigne?.ToLowerInvariant(),
            CoordonneesJson = ligne.CoordonneesJson,
            DC = DateTimeOffset.Now,
            DM = DateTimeOffset.Now
        }, token);

        await _depot.SaveChangesAsync(token);

        return Ok(new { id = entity.Entity.Id });
    }

    /// <summary>
    /// Modifier une ligne de tubelure
    /// </summary>
    /// <param name="id">L'identifiant de l'érablière</param>
    /// <param name="ligne">La ligne à modifier</param>
    /// <param name="token">Le token d'annulation</param>
    /// <response code="204">La ligne a été correctement modifiée.</response>
    /// <response code="400">La validation de la ligne a échoué.</response>
    [HttpPut]
    [ValiderOwnership("id")]
    public async Task<IActionResult> Modifier(Guid id, PutLigneTubelure ligne, CancellationToken token)
    {
        if (id != ligne.IdErabliere)
        {
            return BadRequest("L'id de la route ne concorde pas avec l'id de la ligne à modifier.");
        }

        if (ligne.Id == null)
        {
            return BadRequest("L'id de la ligne à modifier est requis.");
        }

        var erreur = ValiderLigne(ligne.TypeLigne, ligne.CoordonneesJson);

        if (erreur != null)
        {
            return BadRequest(erreur);
        }

        var entity = await _depot.LignesTubelure.FindAsync([ligne.Id], token);

        if (entity == null || entity.IdErabliere != id)
        {
            return NotFound();
        }

        entity.Nom = ligne.Nom;
        entity.TypeLigne = ligne.TypeLigne?.ToLowerInvariant();
        entity.CoordonneesJson = ligne.CoordonneesJson;
        entity.DM = DateTimeOffset.Now;

        await _depot.SaveChangesAsync(token);

        return NoContent();
    }

    /// <summary>
    /// Supprimer une ligne de tubelure. Les entailles raccordées à la ligne sont conservées,
    /// mais leur lien vers la ligne est retiré.
    /// </summary>
    /// <param name="id">Identifiant de l'érablière</param>
    /// <param name="idLigne">Identifiant de la ligne à supprimer</param>
    /// <param name="token">Le token d'annulation</param>
    /// <response code="204">La ligne a été correctement supprimée.</response>
    /// <response code="404">La ligne n'existe pas ou n'appartient pas à l'érablière.</response>
    [HttpDelete("{idLigne}")]
    [ValiderOwnership("id")]
    public async Task<IActionResult> Supprimer(Guid id, Guid idLigne, CancellationToken token)
    {
        var ligne = await _depot.LignesTubelure.FindAsync([idLigne], token);

        if (ligne == null || ligne.IdErabliere != id)
        {
            return NotFound();
        }

        var entailles = await _depot.Entailles
            .Where(e => e.IdLigneTubelure == idLigne)
            .ToListAsync(token);

        foreach (var entaille in entailles)
        {
            entaille.IdLigneTubelure = null;
        }

        _depot.Remove(ligne);

        await _depot.SaveChangesAsync(token);

        return NoContent();
    }

    /// <summary>
    /// Valider le type et les coordonnées d'une ligne de tubelure.
    /// </summary>
    /// <returns>Un message d'erreur, ou null si la ligne est valide</returns>
    internal static string? ValiderLigne(string? typeLigne, string? coordonneesJson)
    {
        if (!TypeLigneTubelure.EstValide(typeLigne))
        {
            return $"Le type de ligne doit être une des valeurs suivantes : {string.Join(", ", TypeLigneTubelure.Tous)}.";
        }

        if (string.IsNullOrWhiteSpace(coordonneesJson))
        {
            return "Les coordonnées de la ligne sont requises.";
        }

        double[][]? coordonnees;

        try
        {
            coordonnees = JsonSerializer.Deserialize<double[][]>(coordonneesJson);
        }
        catch (JsonException)
        {
            return "Les coordonnées de la ligne doivent être un tableau JSON de paires [longitude, latitude].";
        }

        if (coordonnees == null || coordonnees.Length < 2)
        {
            return "Une ligne de tubelure doit contenir au moins 2 points.";
        }

        if (coordonnees.Length > MaxPoints)
        {
            return $"Une ligne de tubelure ne peut pas contenir plus de {MaxPoints} points.";
        }

        foreach (var point in coordonnees)
        {
            if (point == null || point.Length != 2)
            {
                return "Chaque point de la ligne doit être une paire [longitude, latitude].";
            }

            if (point[0] < -180 || point[0] > 180 || point[1] < -90 || point[1] > 90)
            {
                return "Chaque point doit avoir une longitude entre -180 et 180 et une latitude entre -90 et 90.";
            }
        }

        return null;
    }
}
