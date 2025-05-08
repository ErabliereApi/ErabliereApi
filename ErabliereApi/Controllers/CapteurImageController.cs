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

namespace ErabliereApi.Controllers
{
    /// <summary>
    /// Controller représentant les capteurs d'image
    /// </summary>
    /// 
    [Route("Erablieres/{id}/[controller]")]
    [ApiController]
    [Authorize]
    public class CapteurImageController : ControllerBase
    {
        private readonly ErabliereDbContext _depot;
        private readonly IMapper _mapper;
        private readonly IConfiguration _config;

        /// <summary>
        /// Constructeur par initialisation
        /// </summary>
        /// <param name="depot">Le dépôt des capteurs d'image</param>
        /// <param name="mapper">Interface de mapping entre les données</param>
        /// <param name="config">Configuration de l'application</param>
        public CapteurImageController(ErabliereDbContext depot, IMapper mapper, IConfiguration config)
        {
            _depot = depot;
            _mapper = mapper;
            _config = config;
        }

        /// <summary>
        /// Lister les capteurs d'image
        /// </summary>
        /// <param name="id">L'identifiant de l'érablière</param>
        /// <param name="filtreNom">Le filre appliqué sur le nom du capteur</param>
        /// <param name="token">Le jeton d'annulation de la requête</param>
        /// <response code="200">Les capteurs d'image ont correctement été récupérés.</response>
        [HttpGet]
        [ValiderOwnership("id")]
        [EnableQuery]
        public async Task<IEnumerable<GetCapteurImage>> Lister(Guid id, string? filtreNom, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(_config["EmailImageObserverUrl"]))
            {
                return [];
            }

            return await _depot.CapteurImage.AsNoTracking()
                                .Where(b => b.IdErabliere == id &&
                                        (filtreNom == null || (b.Nom != null && b.Nom.Contains(filtreNom))))
                                .ProjectTo<GetCapteurImage>(_mapper.ConfigurationProvider)
                                .ToArrayAsync(token);
        }


        /// <summary>
        /// Ajouter un capteur d'image
        /// </summary>
        /// <param name="id">L'identifiant de l'érablière</param>
        /// <param name="capteur">Le capteur a ajouter</param>
        /// <param name="token">Le jeton d'annulation de la requête</param>
        /// <response code="200">Le capteur a été correctement ajouté.</response>
        [HttpPost]
        [ValiderOwnership("id")]
        public async Task<IActionResult> Ajouter(Guid id, PostCapteurImage capteur, CancellationToken token)
        {
            var capteurImage = _mapper.Map<CapteurImage>(capteur);

            capteurImage.Ordre = await _depot.CapteurImage.Where(c => id == c.IdErabliere).CountAsync(token);
            capteurImage.IdErabliere = id;
            if (string.IsNullOrEmpty(capteurImage.MotDePasse))
            {
                capteurImage.MotDePasse = null;
            }
            if (string.IsNullOrEmpty(capteurImage.Identifiant))
            {
                capteurImage.Identifiant = null;
            }

            var entity = await _depot.CapteurImage.AddAsync(capteurImage, token);




            await _depot.SaveChangesAsync(token);

            return Ok(new { id = entity.Entity.Id });
        }

        /// <summary>
        /// Modifier un capteur d'image
        /// </summary>
        /// <param name="id">L'identifiant de l'érablière</param>
        /// <param name="idCapteur">L'identifiant du capteur à modifier</param>
        /// <param name="capteur">Le capteur a ajouter</param>
        /// <param name="token">Le jeton d'annulation de la requête</param>
        /// <response code="200">Le capteur a été correctement ajouté.</response>
        /// <response code="404">Le capteur n'a pas été trouvé.</response>
        [HttpPut("{idCapteur}")]
        [ValiderOwnership("id")]
        public async Task<IActionResult> Modifier(Guid id, Guid idCapteur, PutCapteurImage capteur, CancellationToken token)
        {
            var capteurImageEntity = await _depot.CapteurImage.FindAsync([idCapteur], cancellationToken: token);
            Dictionary<string, string> errors = [];

            if (capteurImageEntity is null)
            {
                return NotFound("Le capteur d'images à modifier n'existe pas");
            }

            if (string.IsNullOrEmpty(capteur.Nom))
            {
                errors.Add("nom", "Le nom est vide");
            }
            if (string.IsNullOrEmpty(capteur.Url))
            {
                errors.Add("url", "L'url est vide");
            }
            if (string.IsNullOrEmpty(capteur.Port))
            {
                errors.Add("port", "Le port est vide");
            }

            if (errors.Count > 0)
            {
                return BadRequest(errors);
            }

            capteurImageEntity.Nom = capteur.Nom;
            capteurImageEntity.Url = capteur.Url;
            capteurImageEntity.Port = capteur.Port;
            capteurImageEntity.Identifiant = string.IsNullOrEmpty(capteur.Identifiant) ? null : capteur.Identifiant;
            capteurImageEntity.MotDePasse = string.IsNullOrEmpty(capteur.MotDePasse) ? null : capteur.MotDePasse;

            _depot.Update(capteurImageEntity);

            await _depot.SaveChangesAsync(token);

            return Ok();
        }

        /// <summary>
        /// Supprimer un capteur d'image
        /// </summary>
        /// <param name="id">Identifiant de l'érablière</param>
        /// <param name="idCapteur">L'id du capteur d'image à supprimer</param>
        /// <param name="token">Le jeton d'annulation de la requête</param>
        /// <response code="202">Le capteur a été correctement supprimé.</response>
        /// <response code="400">L'id de la route ne concorde pas avec l'id du capteur à supprimer.</response>
        [HttpDelete("{idCapteur}")]
        [ValiderOwnership("id")]
        public async Task<IActionResult> Supprimer(Guid id, Guid idCapteur, CancellationToken token)
        {
            var capteurEntity = await _depot.CapteurImage
                .FirstOrDefaultAsync(x => x.Id == idCapteur && x.IdErabliere == id, token);

            if (capteurEntity is not null)
            {
                _depot.Remove(capteurEntity);

                await _depot.SaveChangesAsync(token);
            }

            return NoContent();
        }
    }
}
