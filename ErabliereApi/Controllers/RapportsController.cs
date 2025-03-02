using System.Globalization;
using System.Text.Json;
using ErabliereApi.Attributes;
using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Donnees.Action.Post;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;

namespace ErabliereApi.Controllers;

/// <summary>
/// Contrôler représentant les données des dompeux
/// </summary>
[ApiController]
[Route("Erablieres/{id}/[controller]")]
[Authorize]
public class RapportsController : ControllerBase
{
    private readonly ErabliereDbContext _context;
    private readonly ILogger<RapportsController> _logger;

    /// <summary>
    /// Constructeur par initialisation
    /// </summary>
    public RapportsController(ErabliereDbContext context, ILogger<RapportsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Retourne la liste des rapports sauvegardés
    /// </summary>
    /// <param name="id"></param>
    [EnableQuery]
    [HttpGet]
    [ValiderOwnership("id")]
    [ProducesResponseType(200, Type = typeof(List<Rapport>))]
    public IActionResult GetSaveReports([FromRoute] Guid id)
    {
        var query = _context.Rapports
            .Where(r => r.IdErabliere == id);

        return Ok(query);
    }

    /// <summary>
    /// Effectue le rapport de degré jour pour une érablière
    /// en se basant soit sur l'id du capteur ou sur les données de température
    /// provenat du trio de données.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="rapportDegreeJour"></param>
    /// <param name="sauvegarder"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    [HttpPost("[action]")]
    [ValiderOwnership("id")]
    [ProducesResponseType(200, Type = typeof(Rapport))]
    public async Task<IActionResult> RapportDegreeJour([FromRoute] Guid? id,
                                                       [FromBody] PostRapportDegreeJourRequest rapportDegreeJour,
                                                       [FromQuery] bool? sauvegarder,
                                                       CancellationToken token)
    {
        if (id != rapportDegreeJour.IdErabliere)
        {
            return BadRequest($"L'id de la route '{id}' ne concorde pas avec l'id de l'érablière du rapport demandé '{rapportDegreeJour.IdErabliere}'.");
        }

        Rapport rapport;

        if (rapportDegreeJour.IdRapport.HasValue)
        {
            throw new NotImplementedException("La mise à jour de rapport n'est pas implémentée.");
        }
        else
        {
            rapport = new Rapport
            {
                Type = "Degré jour",
                DateDebut = rapportDegreeJour.DateDebut,
                DateFin = rapportDegreeJour.DateFin,
                SeuilTemperature = rapportDegreeJour.SeuilTemperature,
                UtiliserTemperatureTrioDonnee = rapportDegreeJour.UtiliserTemperatureTrioDonnee,
                RequestParameters = JsonSerializer.Serialize(rapportDegreeJour),
                AfficherDansDashboard = rapportDegreeJour.AfficherDansDashboard,
                DC = DateTimeOffset.Now,
                IdErabliere = id
            };
        }


        if (rapportDegreeJour.UtiliserTemperatureTrioDonnee)
        {
            var triodonnees = await _context.Donnees
                .Where(d => d.IdErabliere == rapportDegreeJour.IdErabliere && d.D >= rapportDegreeJour.DateDebut && d.D <= rapportDegreeJour.DateFin)
                .OrderBy(d => d.D)
                .ToListAsync(token);

            var memoireDegreeJour = 0m;

            // Code pour le rapport
            // Pour chaque jour, calculer la température moyenne et le degré jour
            // Ajouter les données au rapport
            foreach (var donneesJour in triodonnees.GroupBy(d => d.D?.Date))
            {
                if (donneesJour.Key == DateTime.Today.Date) 
                {
                    continue;
                }

                // Code pour le rapport
                // Pour chaque jour, calculer la température moyenne et le degré jour
                // Ajouter les données au rapport
                var rapportJour = new RapportDonnees
                {
                    Date = donneesJour.Key ?? DateTime.MinValue,
                    Moyenne = (decimal)donneesJour.Average(d => d.T.GetValueOrDefault() / 10.0),
                    Min = (decimal)donneesJour.Min(d => d.T.GetValueOrDefault() / 10.0),
                    Max = (decimal)donneesJour.Max(d => d.T.GetValueOrDefault() / 10.0)
                };

                var degreeJour = Math.Max(0, rapportJour.Moyenne - rapportDegreeJour.SeuilTemperature);

                memoireDegreeJour += degreeJour;

                rapportJour.Somme = memoireDegreeJour;

                rapport.Donnees.Add(rapportJour);
            }
        }
        else
        {
            var capteur = await _context.Capteurs
                .FirstOrDefaultAsync(c => c.Id == rapportDegreeJour.IdCapteur && c.IdErabliere == id, token);

            if (capteur == null)
            {
                ModelState.AddModelError(nameof(rapportDegreeJour.IdCapteur), "Le capteur n'existe pas ou n'appartient pas à l'érablière.");

                return BadRequest(new ValidationProblemDetails(ModelState));
            }

            var donnees = await _context.DonneesCapteur
                .Where(d => d.IdCapteur == rapportDegreeJour.IdCapteur && d.D >= rapportDegreeJour.DateDebut && d.D <= rapportDegreeJour.DateFin)
                .OrderBy(d => d.D)
                .ToListAsync(token);

            var memoireDegreeJour = 0m;

            // Code pour le rapport
            // Pour chaque jour, calculer la température moyenne et le degré jour
            // Ajouter les données au rapport
            foreach (var donneesJour in donnees.GroupBy(d => d.D?.Date))
            {
                if (donneesJour.Key == DateTime.Today.Date) 
                {
                    continue;
                }

                // Code pour le rapport
                // Pour chaque jour, calculer la température moyenne et le degré jour
                // Ajouter les données au rapport
                var rapportJour = new RapportDonnees
                {
                    Date = donneesJour.Key ?? DateTime.MinValue,
                    Moyenne = donneesJour.Average(d => d.Valeur.GetValueOrDefault()),
                    Min = donneesJour.Min(d => d.Valeur.GetValueOrDefault()),
                    Max = donneesJour.Max(d => d.Valeur.GetValueOrDefault())
                };

                var degreeJour = Math.Max(0, rapportJour.Moyenne - rapportDegreeJour.SeuilTemperature);

                memoireDegreeJour += degreeJour;

                rapportJour.Somme = memoireDegreeJour;

                rapport.Donnees.Add(rapportJour);
            }
        }

        rapport.Max = Math.Round(rapport.Donnees.Max(d => d.Max));
        rapport.Min = Math.Round(rapport.Donnees.Min(d => d.Min));
        rapport.Moyenne = Math.Round(rapport.Donnees.Average(d => d.Moyenne));
        rapport.Somme = Math.Round(rapport.Donnees.LastOrDefault()?.Somme ?? 0);

        if (sauvegarder == true)
        {
            await _context.Rapports.AddAsync(rapport, token);

            await _context.TrySaveChangesAsync(token, _logger);
        }

        return Ok(rapport);
    }

    /// <summary>
    /// Supprimer un rapport
    /// </summary>
    /// <param name="id"></param>
    /// <param name="idRapport"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    [HttpDelete("{idRapport}")]
    [ValiderOwnership("id")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> DeleteRapport([FromRoute] Guid id, [FromRoute] Guid idRapport, CancellationToken token)
    {
        var rapport = await _context.Rapports.Include(r => r.Donnees).FirstOrDefaultAsync(r => r.Id == idRapport, token);

        if (rapport == null)
        {
            return NotFound();
        }

        if (id != rapport?.IdErabliere)
        {
            return BadRequest($"L'id de la route '{id}' ne concorde pas avec l'id de l'érablière du rapport demandé '{rapport?.IdErabliere}'.");
        }

        _context.Rapports.Remove(rapport);

        await _context.SaveChangesAsync(token);

        return NoContent();
    }
}
