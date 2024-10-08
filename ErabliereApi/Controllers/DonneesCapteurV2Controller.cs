using AutoMapper;
using AutoMapper.QueryableExtensions;
using ErabliereApi.Attributes;
using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Donnees.Action.Post;
using ErabliereApi.Donnees.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace ErabliereApi.Controllers;

/// <summary>
/// Contrôler représentant les données des dompeux
/// </summary>
[ApiController]
[Route("Capteurs/{id}/[controller]")]
[Authorize]
public class DonneesCapteurV2Controller : ControllerBase
{
    private readonly ErabliereDbContext _depot;
    private readonly IMapper _mapper;

    /// <summary>
    /// Constructeur par initialisation
    /// </summary>
    /// <param name="depot">Le dépôt des barils</param>
    /// <param name="mapper">Interface de mapping entre les objets</param>
    public DonneesCapteurV2Controller(ErabliereDbContext depot, IMapper mapper)
    {
        _depot = depot;
        _mapper = mapper;
    }

    /// <summary>
    /// Liste les données d'un capteur
    /// </summary>
    /// <param name="id">Identifiant du capteur</param>
    /// <param name="ddr">Date de la dernière données reçu. Permet au client d'optimiser le nombres de données reçu.</param>
    /// <param name="dd">Date de début</param>
    /// <param name="df">Date de fin</param>
    /// <param name="order">Ordre de tri</param>
    /// <param name="top">Nombre de données à retourner</param>
    /// <param name="token">Token d'annulation</param>
    /// <response code="200">Une liste de DonneesCapteur.</response>
    [HttpGet]
    [ValiderOwnership("id", typeof(Capteur))]
    [AllowAnonymous]
    public async Task<IEnumerable<GetDonneesCapteurV2>> Lister(Guid id,
                                                              [FromHeader(Name = "x-ddr")] DateTimeOffset? ddr,
                                                              [FromQuery] DateTimeOffset? dd,
                                                              [FromQuery] DateTimeOffset? df,
                                                              [FromQuery][MaxLength(5)] string? order,
                                                              [FromQuery]int? top,
                                                              CancellationToken token)
    {
        var donneesQuery = _depot.DonneesCapteur.AsNoTracking()
                            .Where(b => b.IdCapteur == id &&
                                        (ddr == null || b.D >= ddr) &&
                                        (dd == null || b.D >= dd) &&
                                        (df == null || b.D <= df));

        if (order?.Trim().ToLower() == "desc")
        {
            donneesQuery = donneesQuery.OrderByDescending(b => b.D);
        }
        else
        {
            donneesQuery = donneesQuery.OrderBy(b => b.D);
        }

        if (top.HasValue)
        {
            donneesQuery = donneesQuery.Take(top.Value);
        }

        var donnees = await donneesQuery.ProjectTo<GetDonneesCapteurV2>(_mapper.ConfigurationProvider).ToArrayAsync(token);

        if (donnees.Length > 0)
        {
            if (ddr.HasValue)
            {
                HttpContext.Response.Headers.Append("x-ddr", ddr.Value.ToString("s", CultureInfo.InvariantCulture));
            }

            if (donnees[^1].D.HasValue)
            {
                HttpContext.Response.Headers.Append("x-dde", donnees[^1].D!.Value.ToString("s", CultureInfo.InvariantCulture));
            }
        }

        return donnees;
    }

    /// <summary>
    /// Liste les données de plusieurs capteurs capteurs
    /// </summary>
    /// <param name="ids">Les identifiant des capteurs séparé par des ;</param>
    /// <param name="ddr">Date de la dernière données reçu. Permet au client d'optimiser le nombres de données reçu.</param>
    /// <param name="dd">Date de début</param>
    /// <param name="df">Date de fin</param>
    /// <param name="token">Token d'annulation</param>
    /// <response code="200">Une liste Tupple avec l'id du catpeur et la liste des DonneesCapteur.</response>
    [HttpGet]
    [Route("/Erablieres/{id}/Capteurs/[controller]/Grape")]
    [ValiderOwnership("id")]
    [ProducesResponseType(typeof(IEnumerable<Pair<Guid, IEnumerable<GetDonneesCapteurV2>>>), 200)]
    public async Task<IActionResult> ListerPlusieurs(
                                                [FromQuery] string ids,
                                                [FromHeader(Name = "x-ddr")] DateTimeOffset? ddr,
                                                DateTimeOffset? dd,
                                                DateTimeOffset? df,
                                                CancellationToken token)
    {
        var idsList = ids.Split(';');

        var list = new List<Pair<Guid, IEnumerable<GetDonneesCapteurV2>>>(idsList.Length);

        foreach (var idstr in idsList)
        {
            var id = Guid.Parse(idstr);

            var item = await Lister(id, ddr, dd, df, null, null, token);

            list.Add(new Pair<Guid, IEnumerable<GetDonneesCapteurV2>>(id, item));
        }

        return Ok(list);
    }

    /// <summary>
    /// Ajouter une données d'un capteur
    /// </summary>
    /// <param name="id">L'identifiant du capteurs</param>
    /// <param name="donneeCapteur">Le capteur a ajouter</param>
    /// <param name="token">Token d'annulation</param>
    /// <response code="200">Le capteur a été correctement ajouté.</response>
    /// <response code="400">L'id de la route ne concorde pas avec l'id du capteur à ajouter.</response>
    [HttpPost]
    [TriggerAlertV3]
    [ValiderOwnership("id", typeof(Capteur))]
    public async Task<IActionResult> Ajouter(Guid id, [FromBody] PostDonneeCapteurV2 donneeCapteur, CancellationToken token)
    {
        if (id != donneeCapteur.IdCapteur)
        {
            return BadRequest("L'id de la route ne concorde pas avec l'id du capteur à ajouter");
        }

        if (donneeCapteur.D == null)
        {
            donneeCapteur.D = DateTimeOffset.Now;
        }

        var newDonnee = _mapper.Map<DonneeCapteur>(donneeCapteur);

        await _depot.DonneesCapteur.AddAsync(newDonnee, token);

        await _depot.SaveChangesAsync(token);

        return Ok();
    }

    /// <summary>
    /// Action permetant de créer plusieurs données capteurs
    /// </summary>
    [HttpPost("/Erablieres/{id}/Capteurs/[controller]/PostMany")]
    [TriggerAlertV4]
    [ValiderOwnership("id")]
    public async Task<IActionResult> PostMany(
        [FromRoute] Guid id, [FromBody] PostDonneeCapteurV2[] donnees, CancellationToken token)
    {
        var d = DateTimeOffset.Now;

        foreach (var donnee in donnees)
        {
            if (donnee.D == null)
            {
                donnee.D = d;
            }

            var any = await _depot.Capteurs.AsNoTracking().AnyAsync(c => c.IdErabliere == id &&
                                                                         c.Id == donnee.IdCapteur, token);

            if (any) {
                var newDonnee = _mapper.Map<DonneeCapteur>(donnee);

                await _depot.DonneesCapteur.AddAsync(newDonnee, token);
            }
            else {
                return new NotFoundObjectResult(new {
                    message = $"Le capteur {donnee.IdCapteur} n'existe pas dans l'erablière {id}"
                });
            }
        }

        var count = await _depot.SaveChangesAsync(token);

        return Ok(new {
            count
        });
    }
}
