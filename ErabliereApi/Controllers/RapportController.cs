using ErabliereApi.Attributes;
using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees.Action.Post;
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
public class RapportController : ControllerBase
{
    private readonly ErabliereDbContext _context;

    /// <summary>
    /// Constructeur par initialisation
    /// </summary>
    public RapportController(ErabliereDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Effectue le rapport de degré jour pour une érablière
    /// en se basant soit sur l'id du capteur ou sur les données de température
    /// provenat du trio de données.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="rapportDegreeJour"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    [HttpPost("[action]")]
    [ValiderOwnership("id")]
    [ProducesResponseType(200, Type = typeof(PostRapportDegreeJourResponse))]
    public async Task<IActionResult> RapportDegreeJour([FromRoute] Guid? id,
                                                       [FromBody] PostRapportDegreeJourRequest rapportDegreeJour,
                                                       CancellationToken token)
    {
        if (id != rapportDegreeJour.IdErabliere)
        {
            return BadRequest($"L'id de la route '{id}' ne concorde pas avec l'id de l'érablière du rapport demandé '{rapportDegreeJour.IdErabliere}'.");
        }

        var rapport = new PostRapportDegreeJourResponse
        {
            Requete = rapportDegreeJour
        };

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
                // Code pour le rapport
                // Pour chaque jour, calculer la température moyenne et le degré jour
                // Ajouter les données au rapport
                var rapportJour = new PostRapportDegreeJourResponse.RapportDegreeJour
                {
                    Date = donneesJour.Key?.ToString("yyyy-MM-dd") ?? "date inconnue",
                    Temperature = (decimal)donneesJour.Average(d => d.T.GetValueOrDefault() / 10.0)
                };

                var degreeJour = Math.Max(0, rapportJour.Temperature - rapportDegreeJour.SeuilTemperature);

                memoireDegreeJour += degreeJour;

                rapportJour.DegreJour = memoireDegreeJour;

                rapport.Rapport.Add(rapportJour);
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
                // Code pour le rapport
                // Pour chaque jour, calculer la température moyenne et le degré jour
                // Ajouter les données au rapport
                var rapportJour = new PostRapportDegreeJourResponse.RapportDegreeJour
                {
                    Date = donneesJour.Key?.ToString("yyyy-MM-dd") ?? "date inconnue",
                    Temperature = donneesJour.Average(d => d.Valeur.GetValueOrDefault() / 10m)
                };

                var degreeJour = Math.Max(0, rapportJour.Temperature - rapportDegreeJour.SeuilTemperature);

                memoireDegreeJour += degreeJour;

                rapportJour.DegreJour = memoireDegreeJour;

                rapport.Rapport.Add(rapportJour);
            }
        }

        return Ok(rapport);
    }
}
