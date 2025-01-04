using AutoMapper;
using AutoMapper.QueryableExtensions;
using ErabliereApi.Attributes;
using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Donnees.Action.Get;
using ErabliereApi.Donnees.Action.Post;
using ErabliereApi.Donnees.Action.Put;
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
public class CapteursController : ControllerBase
{
    private readonly ErabliereDbContext _depot;
    private readonly IMapper _mapper;

    /// <summary>
    /// Constructeur par initialisation
    /// </summary>
    /// <param name="depot">Le dépôt des barils</param>
    /// <param name="mapper">Interface de mapping entre les données</param>
    public CapteursController(ErabliereDbContext depot, IMapper mapper)
    {
        _depot = depot;
        _mapper = mapper;
    }

    /// <summary>
    /// Liste les capteurs
    /// </summary>
    /// <param name="id">Identifiant de l'érablière</param>
    /// <param name="filtreNom">Permet de filtrer les capteurs recherché selon leur nom</param>
    /// <param name="token">Le jeton d'annulation</param>
    /// <response code="200">Une liste de capteurs.</response>
    [HttpGet]
    [ValiderOwnership("id")]
    [EnableQuery]
    public async Task<IEnumerable<GetCapteur>> Lister(Guid id, string? filtreNom, CancellationToken token)
    {
        return await _depot.Capteurs.AsNoTracking()
                            .Where(b => b.IdErabliere == id &&
                                    (filtreNom == null || (b.Nom != null && b.Nom.Contains(filtreNom))))
                            .ProjectTo<GetCapteur>(_mapper.ConfigurationProvider)
                            .ToArrayAsync(token);
    }

    /// <summary>
    /// Obtenir un capteur
    /// </summary>
    /// <param name="id">Identifiant de l'érablière</param>
    /// <param name="idCapteur">Id du capteur présent dans la route</param>
    /// <param name="token">Le jeton d'annulation</param>
    /// <response code="200">Une liste de capteurs.</response>
    [HttpGet("{idCapteur}")]
    [ProducesResponseType(200, Type = typeof(GetCapteur))]
    [ValiderOwnership("id")]
    public async Task<IActionResult> Lister(Guid id, Guid idCapteur, CancellationToken token)
    {
        var capteur = await _depot.Capteurs.AsNoTracking()
                            .Where(b => b.IdErabliere == id && b.Id == idCapteur)
                            .ProjectTo<GetCapteur>(_mapper.ConfigurationProvider)
                            .FirstOrDefaultAsync(token);

        if (capteur == null)
        {
            return NotFound();
        }

        return Ok(capteur);
    }

    /// <summary>
    /// Ajouter un capteur
    /// </summary>
    /// <param name="id">L'identifiant de l'érablière</param>
    /// <param name="capteur">Le capteur a ajouter</param>
    /// <param name="token">Le jeton d'annulation de la requête</param>
    /// <response code="200">Le capteur a été correctement ajouté.</response>
    /// <response code="400">L'id de la route ne concorde pas avec l'id du capteur à ajouter.</response>
    [HttpPost]
    [ValiderOwnership("id")]
    public async Task<IActionResult> Ajouter(Guid id, PostCapteur capteur, CancellationToken token)
    {
        if (id != capteur.IdErabliere)
        {
            return BadRequest("L'id de la route n'est pas le même que l'id de l'érablière dans les données du capteur à ajouter");
        }

        if (capteur.Id.HasValue && await _depot.Capteurs.AnyAsync(c => c.Id == capteur.Id, token))
        {
            return BadRequest($"Le capteur avec l'id '{capteur.Id}' exite déjà");
        }

        if (!capteur.DC.HasValue)
        {
            capteur.DC = DateTimeOffset.Now;
        }

        var entity = await _depot.Capteurs.AddAsync(_mapper.Map<Capteur>(capteur), token);

        entity.Entity.IndiceOrdre = await _depot.Capteurs.Where(c => capteur.IdErabliere == c.IdErabliere).CountAsync(token);

        await _depot.SaveChangesAsync(token);

        return Ok(new { id = entity.Entity.Id });
    }

    /// <summary>
    /// Action pour ajouter une liste de capteurs à chaque erabliere d'un filtre
    /// </summary>
    /// <param name="nameLike">Permet de filtrer les érablières recherchées selon leur nom</param>
    /// <param name="dcNull">Permet de filtrer les érablières qui n'ont pas de date de création</param>
    /// <param name="capteurs">La liste de capteurs à ajouter</param>
    /// <param name="token">Le jeton d'annulation</param>
    /// <response code="204">Les capteurs ont été correctement ajoutés.</response>
    [HttpPost("/Capteurs/AjouterListe")]
    [Authorize(Roles = "administrateur", Policy = "TenantIdPrincipal")]
    public async Task<IActionResult> AjouterListe([FromQuery] string? nameLike, [FromQuery] bool? dcNull, [FromBody] PostCapteur[] capteurs, CancellationToken token)
    {
        var query = _depot.Erabliere.AsQueryable();

        if (dcNull.HasValue)
        {
            if (dcNull.Value)
            {
                query = query.Where(e => e.DC == null);
            }
            else
            {
                query = query.Where(e => e.DC != null);
            }
        }

        if (!string.IsNullOrWhiteSpace(nameLike))
        {
            query = query.Where(e => EF.Functions.Like(e.Nom, $"%{nameLike}%"));
        }

        var erablieres = await query.ToArrayAsync(token);

        foreach (var erabliere in erablieres)
        {
            foreach (var capteur in capteurs)
            {
                capteur.IdErabliere = erabliere.Id;

                await _depot.Capteurs.AddAsync(_mapper.Map<Capteur>(capteur), token);
            }
        }

        await _depot.SaveChangesAsync(token);

        return NoContent();
    }

    /// <summary>
    /// Modifier une liste de capteurs
    /// </summary>
    /// <param name="id">L'identifiant de l'érablière</param>
    /// <param name="capteurs">La liste de capteurs à modifier</param>
    /// <param name="token">Le jeton d'annulation</param>
    /// <response code="204">Les capteurs ont été correctement modifiés.</response>
    /// <response code="400">L'id de la route ne concorde pas avec l'id du capteur à modifier.</response>
    [HttpPut]
    [ValiderOwnership("id")]
    public async Task<IActionResult> ModifierListe(Guid id, PutCapteur[] capteurs, CancellationToken token)
    {
        foreach (var capteur in capteurs)
        {
            if (id != capteur.IdErabliere)
            {
                return BadRequest("L'id de la route ne concorde pas avec l'id de l'érablière possédant le capteur à modifier.");
            }

            var capteurEntity = await _depot.Capteurs.FindAsync([capteur.Id], cancellationToken: token);

            if (capteurEntity == null)
            {
                return NotFound($"Le capteur avec l'id {capteur.Id} n'existe pas.");
            }

            var result = UpdateCapteur(capteur, capteurEntity);

            if (result is not NoContentResult)
            {
                return result;
            }
        }

        await _depot.SaveChangesAsync(token);

        return NoContent();
    }

    /// <summary>
    /// Modifier un capteur
    /// </summary>
    /// <param name="id">L'identifiant de l'érablière</param>
    /// <param name="idCapteur">L'identifiant du capteur à modifier</param>
    /// <param name="capteur">Le capteur à modifier</param>
    /// <param name="token">Le jeton d'annulation</param>
    /// <response code="204">Le capteur a été correctement modifié.</response>
    /// <response code="400">L'id de la route ne concorde pas avec l'id du capteur à modifier.</response>
    /// <response code="404">Le capteur n'existe pas.</response>
    [HttpPut("{idCapteur}")]
    [ValiderOwnership("id")]
    public async Task<IActionResult> Modifier(Guid id, Guid idCapteur, PutCapteur capteur, CancellationToken token)
    {
        if (id != capteur.IdErabliere)
        {
            return BadRequest("L'id de la route ne concorde pas avec l'id de l'érablière possédant le capteur à modifier.");
        }

        if (idCapteur != capteur.Id)
        {
            return BadRequest("L'id de la route ne concorde pas avec l'id du capteur à modifier.");
        }

        var capteurEntity = await _depot.Capteurs.FindAsync([capteur.Id], cancellationToken: token);

        if (capteurEntity == null)
        {
            return NotFound($"Le capteur avec l'id {capteur.Id} n'existe pas.");
        }

        var result = UpdateCapteur(capteur, capteurEntity);

        if (result is NoContentResult)
        {
            await _depot.SaveChangesAsync(token);
        }

        return result;
    }

    /// <summary>
    /// Modifier un capteur depuis son identifiant externe
    /// </summary>
    /// <param name="id">L'identifiant de l'érablière</param>
    /// <param name="externalId">L'identifiant du système externe du capteur</param>
    /// <param name="capteur">Le capteur à modifier</param>
    /// <param name="token">Le jeton d'annulation</param>
    /// <response code="204">Le capteur a été correctement modifié.</response>
    /// <response code="400">L'id de la route ne concorde pas avec l'id du capteur à modifier.</response>
    /// <response code="404">Le capteur n'existe pas.</response>
    [HttpPut("ExternalId/{externalId}")]
    [ValiderOwnership("id")]
    public async Task<IActionResult> ModifierDepuisIdentifiantExterne(Guid id, string externalId, PutCapteur capteur, CancellationToken token)
    {
        if (id != capteur.IdErabliere)
        {
            return BadRequest("L'id de la route ne concorde pas avec l'id de l'érablière possédant le capteur à modifier.");
        }

        if (string.IsNullOrWhiteSpace(externalId))
        {
            return BadRequest("L'identifiant externe du capteur ne peut pas être vide.");
        }

        var capteurEntities = await _depot.Capteurs.Where(c => c.ExternalId == externalId && c.IdErabliere == id).ToArrayAsync(token);

        if (capteurEntities.Length == 0)
        {
            return NotFound($"Le capteur avec l'identifiant externe {externalId} n'existe pas.");
        }

        foreach (var capteurEntity in capteurEntities)
        {
            var result = UpdateCapteur(capteur, capteurEntity);

            if (result is not NoContentResult)
            {
                return result;
            }
        }

        await _depot.SaveChangesAsync(token);

        return NoContent();
    }

    /// <summary>
    /// Permet à un administrateur de modifier plusieurs capteurs depuis leurs nom
    /// </summary>
    /// <param name="filtreNom">Modifier les capteurs avec le nom suivant</param>
    /// <param name="putCapteur">Modification à apporter à chaque capteur</param>
    /// <param name="token"></param>
    /// <returns></returns>
    [HttpPut("/Capteurs/ModifierDepuisFiltre")]
    [Authorize(Roles = "administrateur", Policy = "TenantIdPrincipal")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> ModifierDepuisFiltre([FromQuery] string filtreNom, [FromBody] PutCapteur putCapteur, CancellationToken token)
    {
        var capteurs = await _depot.Capteurs.Where(c => c.Nom == filtreNom).ToArrayAsync(token);

        foreach (var capteur in capteurs)
        {
            var result = UpdateCapteur(putCapteur, capteur);

            if (result is not NoContentResult)
            {
                return result;
            }
        }

        var modified = await _depot.SaveChangesAsync(token);

        return Ok(new {
            modified
        });
    }

    private IActionResult UpdateCapteur(PutCapteur capteur, Capteur capteurEntity)
    {
        if (capteur.AfficherCapteurDashboard.HasValue)
        {
            capteurEntity.AfficherCapteurDashboard = capteur.AfficherCapteurDashboard.Value;
        }

        if (capteur.AjouterDonneeDepuisInterface.HasValue)
        {
            capteurEntity.AjouterDonneeDepuisInterface = capteur.AjouterDonneeDepuisInterface.Value;
        }

        if (capteur.DC.HasValue)
        {
            capteurEntity.DC = capteur.DC;
        }

        if (!string.IsNullOrWhiteSpace(capteur.Nom))
        {
            capteurEntity.Nom = capteur.Nom;
        }

        if (!string.IsNullOrWhiteSpace(capteur.Symbole)) 
        {
            capteurEntity.Symbole = capteur.Symbole;
        }

        if (capteur.IndiceOrdre.HasValue)
        {
            capteurEntity.IndiceOrdre = capteur.IndiceOrdre;
        }

        if (capteur.Taille.HasValue)
        {
            if (capteur.Taille.Value <= 0 || capteur.Taille.Value > 12)
            {
                return BadRequest("La taille du graphique doit être comprise entre 1 et 12");
            }
            capteurEntity.Taille = capteur.Taille.Value;
        }

        if (capteur.BatteryLevel.HasValue)
        {
            capteurEntity.BatteryLevel = capteur.BatteryLevel.Value;
        }

        if (capteur.Type != null)
        {
            capteurEntity.Type = capteur.Type;
        }

        if (capteur.ExternalId != null)
        {
            capteurEntity.ExternalId = capteur.ExternalId;
        }

        if (capteur.Online != null)
        {
            capteurEntity.Online = capteur.Online.Value;
        }

        if (capteur.ReportFrequency.HasValue)
        {
            capteurEntity.ReportFrequency = capteur.ReportFrequency;
        }

        if (capteur.LastMessageTime.HasValue)
        {
            capteurEntity.LastMessageTime = capteur.LastMessageTime;
        }

        if (capteur.DisplayType != null) 
        {
            capteurEntity.DisplayType = capteur.DisplayType;
        }

        if (capteur.DisplayTop.HasValue)
        {
            capteurEntity.DisplayTop = capteur.DisplayTop.Value;
        }

        if (capteur.DisplayMin.HasValue)
        {
            capteurEntity.DisplayMin = capteur.DisplayMin.Value;
        }

        if (capteur.DisplayMax.HasValue)
        {
            capteurEntity.DisplayMax = capteur.DisplayMax.Value;
        }

        _depot.Update(capteurEntity);

        return NoContent();
    }

    /// <summary>
    /// Supprimer un capteur
    /// </summary>
    /// <param name="id">Identifiant de l'érablière</param>
    /// <param name="idCapteur">L'id du capteur à supprimer</param>
    /// <param name="token">Le jeton d'annulation</param>
    /// <response code="202">Le capteur a été correctement supprimé.</response>
    /// <response code="404">Le capteur n'existe pas.</response>
    [HttpDelete("{idCapteur}")]
    [ValiderOwnership("id")]
    public async Task<IActionResult> Supprimer(Guid id, Guid idCapteur, CancellationToken token)
    {
        var capteurEntity = await _depot.Capteurs
            .FirstOrDefaultAsync(x => x.Id == idCapteur && x.IdErabliere == id, token);

        if (capteurEntity != null)
        {
            _depot.Remove(capteurEntity);

            await _depot.SaveChangesAsync(token);
        }
        else
        {
            return NotFound();
        }

        return NoContent();
    }
}
