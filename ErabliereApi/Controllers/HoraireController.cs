using ErabliereApi.Attributes;
using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Donnees.Action.Put;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErabliereApi.Controllers;

/// <summary>
/// Contrôler représentant les données des dompeux
/// </summary>
[ApiController]
[Route("Erablieres/{id}/[controller]")]
[Authorize]
public class HoraireController : ControllerBase
{
    private readonly ErabliereDbContext _dbContext;

    /// <summary>
    /// Constructeur par défaut
    /// </summary>
    /// <param name="dbContext"></param>
    public HoraireController(ErabliereDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Ajout ou modification de l'horaire d'une éralière
    /// </summary>
    /// <param name="id"></param>
    /// <param name="putHoraire"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    [HttpPut]
    [ValiderOwnership("id")]
    public async Task<IActionResult> UpdateHoraire(Guid id, [FromBody] PutHoraire putHoraire, CancellationToken token)
    {
        if (id != putHoraire.IdErabliere)
        {
            return BadRequest("L'ID de l'érablière ne correspond pas à l'ID dans le corps de la requête.");
        }

        var horaire = await _dbContext.Horaires.FirstOrDefaultAsync(h => h.IdErabliere == id, token);

        if (horaire == null)
        {
            // Ajouter une nouvelle horaire
            horaire = new Horaire
            {
                IdErabliere = id,
                Lundi = putHoraire.Lundi,
                Mardi = putHoraire.Mardi,
                Mercredi = putHoraire.Mercredi,
                Jeudi = putHoraire.Jeudi,
                Vendredi = putHoraire.Vendredi,
                Samedi = putHoraire.Samedi,
                Dimanche = putHoraire.Dimanche
            };

            await _dbContext.Horaires.AddAsync(horaire, token);
        }
        else
        {
            // Mise à jour de l'horaire
            horaire.Lundi = putHoraire.Lundi;
            horaire.Mardi = putHoraire.Mardi;
            horaire.Mercredi = putHoraire.Mercredi;
            horaire.Jeudi = putHoraire.Jeudi;
            horaire.Vendredi = putHoraire.Vendredi;
            horaire.Samedi = putHoraire.Samedi;
            horaire.Dimanche = putHoraire.Dimanche;
        }

        await _dbContext.SaveChangesAsync(token);

        return NoContent();
    }
}