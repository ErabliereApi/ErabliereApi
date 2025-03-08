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
            return BadRequest("L'id du rapport ne doit pas être spécifié lors de la création d'un nouveau rapport.");
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

        IActionResult value = await InnerCalculateDegreeJour(id, rapportDegreeJour, rapport, token);

        if (sauvegarder == true)
        {
            await _context.Rapports.AddAsync(rapport, token);

            await _context.TrySaveChangesAsync(token, _logger);
        }

        return value;
    }

    /// <summary>
    /// Rafraichir un rapport
    /// </summary>
    /// <param name="id"></param>
    /// <param name="idRapport"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    [HttpPatch("[action]{idRapport}/Refresh")]
    [ValiderOwnership("id")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Refresh([FromRoute] Guid id, [FromRoute] Guid idRapport, CancellationToken token)
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

        var rapportDegreeJour = JsonSerializer.Deserialize<PostRapportDegreeJourRequest>(rapport.RequestParameters);

        if (rapportDegreeJour == null)
        {
            return BadRequest("Les paramètres de la requête ne peuvent pas être désérialisés. Ces paramètres sont enregistré dans la base de données. Vous devrez recréer le rapport pour le mettre à jour.");
        }

        var result = await InnerCalculateDegreeJour(id, rapportDegreeJour, rapport, token);

        if (result is OkObjectResult)
        {
            await _context.SaveChangesAsync(token);
        }

        return NoContent();
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

    private async Task<IActionResult> InnerCalculateDegreeJour(Guid? id, PostRapportDegreeJourRequest rapportDegreeJour, Rapport rapport, CancellationToken token)
    {
        if (rapportDegreeJour.UtiliserTemperatureTrioDonnee)
        {
            await InnerCalculateFromTrioDonnees(rapportDegreeJour, rapport, token);
        }
        else
        {
            (bool isErrorActionResult, IActionResult value) = await InnerCalculateFromSensorValues(id, rapportDegreeJour, rapport, token);
            
            if (isErrorActionResult)
            {
                return value;
            }
        }

        rapport.Max = Math.Round(rapport.Donnees.Max(d => d.Max));
        rapport.Min = Math.Round(rapport.Donnees.Min(d => d.Min));
        rapport.Moyenne = Math.Round(rapport.Donnees.Average(d => d.Moyenne));
        rapport.Somme = Math.Round(rapport.Donnees.LastOrDefault()?.Somme ?? 0);

        return Ok(rapport);
    }

    private async Task<(bool isErrorActionResult, IActionResult value)> InnerCalculateFromSensorValues(Guid? id, PostRapportDegreeJourRequest rapportDegreeJour, Rapport rapport, CancellationToken token)
    {
        var capteur = await _context.Capteurs
            .FirstOrDefaultAsync(c => c.Id == rapportDegreeJour.IdCapteur && c.IdErabliere == id, token);

        if (capteur == null)
        {
            ModelState.AddModelError(nameof(rapportDegreeJour.IdCapteur), "Le capteur n'existe pas ou n'appartient pas à l'érablière.");

            return (isErrorActionResult: true, value: BadRequest(new ValidationProblemDetails(ModelState)));
        }

        var dateDebutFiltre = rapportDegreeJour.DateDebut.Date;

        if (rapport.Donnees.Count > 0)
        {
            dateDebutFiltre = rapport.Donnees.Max(d => d.Date).Date.AddDays(1);
        }

        var donnees = await _context.DonneesCapteur
            .Where(donneesCapteur => donneesCapteur.IdCapteur == rapportDegreeJour.IdCapteur && 
                                     donneesCapteur.D >= dateDebutFiltre && 
                                     donneesCapteur.D <= rapportDegreeJour.DateFin)
            .OrderBy(d => d.D)
            .ToListAsync(token);

        var memoireDegreeJour = rapport.Somme;

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

        return (false, Ok(rapport));
    }

    private async Task InnerCalculateFromTrioDonnees(PostRapportDegreeJourRequest rapportDegreeJour, Rapport rapport, CancellationToken token)
    {
        var dateDebutFiltre = rapportDegreeJour.DateDebut.Date;

        if (rapport.Donnees.Count > 0)
        {
            dateDebutFiltre = rapport.Donnees.Max(d => d.Date).Date.AddDays(1);
        }

        var triodonnees = await _context.Donnees
                        .Where(donneesTrio => donneesTrio.IdErabliere == rapportDegreeJour.IdErabliere && 
                                              donneesTrio.D >= dateDebutFiltre && 
                                              donneesTrio.D <= rapportDegreeJour.DateFin)
                        .OrderBy(d => d.D)
                        .ToListAsync(token);

        var memoireDegreeJour = rapport.Somme;

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
}
