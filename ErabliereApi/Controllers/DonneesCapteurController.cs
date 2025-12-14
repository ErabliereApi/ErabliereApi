using ErabliereApi.Attributes;
using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErabliereApi.Controllers;

/// <summary>
/// Contrôler représentant les données des dompeux
/// </summary>
[ApiController]
[Route("Capteurs/{id}/[controller]")]
[Authorize]
public class DonneesCapteurController : ControllerBase
{
    private readonly ErabliereDbContext _depot;

    /// <summary>
    /// Constructeur par initialisation
    /// </summary>
    /// <param name="depot">Le dépôt des barils</param>
    public DonneesCapteurController(ErabliereDbContext depot)
    {
        _depot = depot;
    }

    /// <summary>
    /// Modifier une données d'un capteur
    /// </summary>
    /// <param name="id">L'identifiant du capteur</param>
    /// <param name="capteur">Le capteur a modifier</param>
    /// <param name="token">Token d'annulation</param>
    /// <response code="200">Le capteur a été correctement supprimé.</response>
    /// <response code="400">L'id de la route ne concorde pas avec l'id du capteur à modifier.</response>
    [HttpPut]
    [ValiderOwnership("id", typeof(Capteur))]
    public async Task<IActionResult> Modifier(Guid id, DonneeCapteur capteur, CancellationToken token)
    {
        if (id != capteur.IdCapteur)
        {
            return BadRequest("L'id de la route ne concorde pas avec l'id du capteur à modifier.");
        }

        _depot.Update(capteur);

        await _depot.SaveChangesAsync(token);

        return Ok();
    }

    /// <summary>
    /// Supprimer une données d'un capteur
    /// </summary>
    /// <param name="id">Identifiant du capteur</param>
    /// <param name="capteur">Le capteur a supprimer</param>
    /// <param name="token">Token d'annulation</param>s
    /// <response code="204">Le capteur a été correctement supprimé.</response>
    /// <response code="400">L'id de la route ne concorde pas avec l'id du capteur à supprimer.</response>
    [HttpDelete]
    [ValiderOwnership("id", typeof(Capteur))]
    public async Task<IActionResult> Supprimer(Guid id, DonneeCapteur capteur, CancellationToken token)
    {
        if (id != capteur.IdCapteur)
        {
            return BadRequest("L'id de la route ne concorde pas avec l'id du baril à supprimer.");
        }

        _depot.Remove(capteur);

        await _depot.SaveChangesAsync(token);

        return NoContent();
    }
}
