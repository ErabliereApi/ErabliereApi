using AutoMapper;
using AutoMapper.QueryableExtensions;
using ErabliereApi.Attributes;
using ErabliereApi.Authorization;
using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Donnees.Action.Delete;
using ErabliereApi.Donnees.Action.Get;
using ErabliereApi.Donnees.Action.Post;
using ErabliereApi.Donnees.Action.Put;
using ErabliereApi.Extensions;
using ErabliereApi.Services.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Localization;

namespace ErabliereApi.Controllers;

/// <summary>
/// Contrôleur pour gérer les érablières
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize]
public class ErablieresController : ControllerBase
{
    private readonly ErabliereDbContext _context;
    private readonly IMapper _mapper;
    private readonly IConfiguration _config;
    private readonly IDistributedCache _cache;
    private readonly IServiceProvider _serviceProvider;
    private readonly IStringLocalizer<ErablieresController> _localizer;
    private readonly ILogger<ErablieresController> _logger;

    const string ACCESS_NOT_EMPTY = "AccessCannotBeEmpty";
    const string ACCESS_MUST_BE_IN_0_TO_5 = "AccessMustBeIn0To5";

    /// <summary>
    /// Constructeur par initialisation
    /// </summary>
    /// <param name="context">Classe de contexte pour accéder à la BD</param>
    /// <param name="mapper">mapper de donnée</param>
    /// <param name="config">Permet d'accéder au configuration de l'api</param>
    /// <param name="cache">Cache distribué</param>
    /// <param name="serviceProvider">Service scope</param>
    /// <param name="localizer">Localisateur de ressource</param>
    /// <param name="logger"></param>
    public ErablieresController(
        ErabliereDbContext context,
        IMapper mapper,
        IConfiguration config,
        IDistributedCache cache,
        IServiceProvider serviceProvider,
        IStringLocalizer<ErablieresController> localizer,
        ILogger<ErablieresController> logger)
    {
        _context = context;
        _mapper = mapper;
        _config = config;
        _cache = cache;
        _serviceProvider = serviceProvider;
        _localizer = localizer;
        _logger = logger;
    }

    private const int TakeErabliereNbMax = 20;

    /// <summary>
    /// Liste les érablières
    /// </summary>
    /// <param name="orderby">Ordre de tri</param>
    /// <param name="filter">Filtre</param>
    /// <param name="top">Nombre d'érablière à retourner</param>
    /// <param name="token">Jeton d'annulation de la requête</param>
    /// <returns>Une liste d'érablière</returns>
    [HttpGet]
    [SecureEnableQuery(MaxTop = TakeErabliereNbMax)]
    [AllowAnonymous]
    public async Task<IQueryable<Erabliere>> ListerAsync(
        [FromQuery(Name = "$orderby")] string? orderby,
        [FromQuery(Name = "$filter")] string? filter,
        [FromQuery(Name = "$top")] int? top,
        CancellationToken token)
    {
        var query = _context.Erabliere.AsNoTracking();

        var (isAuthenticate, _, customer) = await IsAuthenticatedAsync(token);

        if (_config.IsAuthEnabled() &&
            (!isAuthenticate))
        {
            query = query.Where(e => e.IsPublic);
        }
        else
        {
            if (isAuthenticate && customer != null)
            {
                Guid?[] erablieresOwned
                    = await _context.CustomerErablieres
                    .AsNoTracking()
                    .Where(c => c.IdCustomer == customer.Id)
                    .Select(c => c.IdErabliere)
                    .ToArrayAsync(token);

                query = query.Where(e => erablieresOwned.Contains(e.Id));
            }
            else if (isAuthenticate && customer == null)
            {
                throw new InvalidOperationException("The user is authenticated, but there is no customer...");
            }
        }

        query = await AddOrderAndPageInfo(orderby, filter, top, query, token);

        return query;
    }

    private async Task<IQueryable<Erabliere>> AddOrderAndPageInfo(string? orderby, string? filter, int? top, IQueryable<Erabliere> query, CancellationToken token)
    {
        HttpContext.Response.Headers.Append("X-ErabliereTotal", (await query.CountAsync(token)).ToString());

        if (string.IsNullOrWhiteSpace(orderby))
        {
            query = query.OrderBy(e => e.IndiceOrdre ?? int.MaxValue).ThenBy(e => e.Nom);
        }

        if (string.IsNullOrWhiteSpace(filter))
        {
            if (top.HasValue)
            {
                if (top > TakeErabliereNbMax)
                {
                    query = query.Take(TakeErabliereNbMax);
                }
            }
            else
            {
                query = query.Take(TakeErabliereNbMax);
            }
        }

        return query;
    }

    /// <summary>
    /// Lister les érablières sous format GeoJson
    /// </summary>
    [HttpGet("GeoJson")]
    [AllowAnonymous]
    public async Task<IActionResult> GetGeoJson(
        [FromQuery] bool? isPublic,
        [FromQuery] bool? my,
        [FromQuery(Name = "capteur")] string? nomCapteurs,
        [FromQuery(Name = "topCapteur")] int? topCapteurs,
        CancellationToken token)
    {
        var (isAuthenticate, _, customer) = await IsAuthenticatedAsync(token);

        if (!isAuthenticate && my.HasValue && my.Value)
        {
            ModelState.AddModelError("my", "Vous devez être connecté pour accéder à vos érablières.");

            return BadRequest(new ValidationProblemDetails(ModelState));
        }

        if (my == null && isPublic == null)
        {
            isPublic = true;
        }

        var erablieresQuery = _context.Erabliere
            .AsNoTracking()
            .Where(e => e.Latitude != 0 && e.Longitude != 0);

        if (isPublic.HasValue && (!my.HasValue || !my.Value))
        {
            erablieresQuery = erablieresQuery.Where(e => e.IsPublic);
        }
        else if (isPublic == false)
        {
            erablieresQuery = erablieresQuery.Where(e => !e.IsPublic);
        }

        if (my.HasValue && my.Value)
        {
            if (isAuthenticate && customer != null)
            {
                Guid?[] erablieresOwned
                    = await _context.CustomerErablieres
                    .AsNoTracking()
                    .Where(c => c.IdCustomer == customer.Id)
                    .Select(c => c.IdErabliere)
                    .ToArrayAsync(token);

                erablieresQuery = erablieresQuery.Where(e => erablieresOwned.Contains(e.Id) ||
                                                             (isPublic.HasValue && isPublic.Value && e.IsPublic));
            }
            else if (isAuthenticate && customer == null)
            {
                throw new InvalidOperationException("The user is authenticated, but there is no customer...");
            }
        }

        if (!string.IsNullOrWhiteSpace(nomCapteurs))
        {
#nullable disable
            erablieresQuery = erablieresQuery
                .Where(e => e.Capteurs.Any(c => EF.Functions.Like(c.Nom, nomCapteurs)));

            erablieresQuery = erablieresQuery
                .Select(e => new Erabliere
                {
                    Id = e.Id,
                    Nom = e.Nom,
                    Longitude = e.Longitude,
                    Latitude = e.Latitude,
                    IsPublic = e.IsPublic,
                    Capteurs = e.Capteurs
                        .Where(c => EF.Functions.Like(c.Nom, nomCapteurs))
                        .OrderBy(c => c.IndiceOrdre)
                        .Take(topCapteurs ?? 1)
                        .Select(c => new Capteur
                        {
                            Id = c.Id,
                            Nom = c.Nom,
                            Symbole = c.Symbole,
                            DonneesCapteur = c.DonneesCapteur
                                .OrderByDescending(dc => dc.D)
                                .Take(1)
                                .Select(dc => new DonneeCapteur
                                {
                                    D = dc.D,
                                    Valeur = dc.Valeur
                                })
                                .ToList()
                        })
                        .ToList()
                });
#nullable enable

            if (string.Equals(_config["GeoJson_FromSingleQuery"]?.Trim(), "true", StringComparison.OrdinalIgnoreCase))
            {
                erablieresQuery = erablieresQuery.AsSingleQuery();
            }
            else 
            {
                erablieresQuery = erablieresQuery.AsSplitQuery();
            }
        }

        var erablieres = new Erabliere[0];

        if (isPublic == false && my == false)
        {
            erablieres = new Erabliere[0];
        }
        else
        {
            erablieres = await erablieresQuery.ToArrayAsync(token);
        }

        var geoJson = new
        {
            type = "FeatureCollection",
            features = erablieres.Select(e => new
            {
                type = "Feature",
                geometry = new
                {
                    type = "Point",
                    coordinates = new double[] { e.Longitude, e.Latitude }
                },
                properties = new
                {
                    name = e.Nom,
                    id = e.Id,
                    capteur = e.Capteurs?.Select(c => new
                    {
                        c.Nom,
                        c.DonneesCapteur.OrderByDescending(d => d.D).FirstOrDefault()?.Valeur,
                        c.Symbole
                    }).ToArray(),
                    isPublic = e.IsPublic
                }
            }).ToArray()
        };

        return Ok(geoJson);
    }

    /// <summary>
    /// Créer une érablière
    /// </summary>
    /// <param name="postErabliere">L'érablière à créer</param>
    /// <param name="token">Le jeton d'annulation de la requête http</param>
    /// <response code="200">L'érablière a été correctement ajouté</response>
    /// <response code="400">
    /// Le nom de l'érablière dépasse les 50 caractères, est null ou vide ou un érablière avec le nom reçu existe déjà.
    /// </response>
    /// <response code="409">La clé primaire de l'érablière existe déjà</response>
    [HttpPost]
    public async Task<IActionResult> Ajouter(PostErabliere postErabliere, CancellationToken token)
    {
        var (isValid, actionResult) = await postErabliere.ValidateAsync(ModelState, _context, _localizer, token);

        if (!isValid)
        {
            if (actionResult != null)
            {
                return actionResult;
            }
            return BadRequest();
        }

        var erabliere = await AddErabliereAsync(postErabliere, token);

        if (erabliere.Item2 != null)
        {
            return erabliere.Item2;
        }

        await _context.SaveChangesAsync(token);

        return Ok(new { id = erabliere.Item1?.Id });
    }

    /// <summary>
    /// Importer des érablières
    /// </summary>
    [HttpPost("[action]")]
    [Authorize(Roles = "administrateur", Policy = "TenantIdPrincipal")]
    public async Task<IActionResult> Import(PostErabliere[] postErablieres, CancellationToken token)
    {
        foreach (var postErabliere in postErablieres)
        {
            var (isValid, actionResult) = await postErabliere.ValidateAsync(ModelState, _context, _localizer, token);

            if (!isValid)
            {
                if (actionResult != null)
                {
                    return actionResult;
                }
                return BadRequest();
            }
        }

        foreach (var postErabliere in postErablieres)
        {
            var erabliere = await AddErabliereAsync(postErabliere, token);

            if (erabliere.Item2 != null)
            {
                return erabliere.Item2;
            }
        }

        var count = await _context.SaveChangesAsync(token);

        return Ok(new
        {
            count
        });
    }

    private async Task<(Erabliere?, IActionResult?)> AddErabliereAsync(PostErabliere postErabliere, CancellationToken token)
    {
        var erabliere = _mapper.Map<Erabliere>(postErabliere);

        var (isAuthenticate, _, customer) = await IsAuthenticatedAsync(token);

        if (isAuthenticate)
        {
            erabliere.IsPublic = false;

            if (postErabliere.IsPublic != erabliere.IsPublic)
            {
                erabliere.IsPublic = postErabliere.IsPublic;
            }
        }
        else
        {
            erabliere.IsPublic = true;

            if (postErabliere.IsPublic != erabliere.IsPublic)
            {
                ModelState.AddModelError("IsPublic", _localizer["EnforceIsPublic"]);

                return (null, BadRequest(new ValidationProblemDetails(ModelState)));
            }
        }

        erabliere.AfficherPredictionMeteoHeure ??= true;
        erabliere.AfficherPredictionMeteoJour ??= true;
        erabliere.DimensionPanneauImage ??= 12;

        erabliere.DC = DateTimeOffset.Now;

        var entity = await _context.Erabliere.AddAsync(erabliere, token);

        if (isAuthenticate)
        {
            if (customer != null)
            {
                await _context.CustomerErablieres.AddAsync(new CustomerErabliere
                {
                    Access = 15,
                    IdCustomer = customer.Id,
                    IdErabliere = entity.Entity.Id,
                    DC = DateTimeOffset.Now
                }, token);
            }
            else
            {
                throw new InvalidOperationException("The user is authenticated, but there is no customer...");
            }
        }

        return (entity.Entity, null);
    }

    private async Task<(bool, string, Customer?)> IsAuthenticatedAsync(CancellationToken token)
    {
        if (User?.Identity?.IsAuthenticated == true)
        {
            using var scope = _serviceProvider.CreateScope();

            var unique_name = UsersUtils.GetUniqueName(scope, User);

            var customer = await _context.Customers.SingleAsync(c => c.UniqueName == unique_name, token);

            return (true, "Bearer", customer);
        }

        if (_config.StripeIsEnabled())
        {
            var apiKeyAuthContext = HttpContext?.RequestServices.GetRequiredService<ApiKeyAuthorizationContext>();

            return (apiKeyAuthContext?.Authorize == true, "ApiKey", apiKeyAuthContext?.Customer);
        }

        return (false, "", null);
    }

    /// <summary>
    /// Modifier une érablière
    /// </summary>
    /// <param name="id">L'id de l'érablière à modifier</param>
    /// <param name="erabliere">Les données de l'érablière à modifier.
    ///     1. L'id doit concorder avec celui de la route.
    ///     2. L'érablière doit exister.
    ///     3. Si le nom est modifié, il ne doit pas être pris par une autre érablière.
    /// 
    /// Pour modifier l'adresse IP, vous devez entrer quelque chose. "-" pour supprimer les règles déjà existante.</param>
    /// <param name="token">Un jeton d'annulation</param>
    /// <response code="200">L'érablière a été correctement modifiée.</response>
    /// <response code="400">Une des validations des paramètres a échoué.</response>
    /// <response code="404">L'érablière n'a pas été trouvée.</response>
    /// <returns></returns>
    [HttpPut("{id}")]
    [ValiderIPRules]
    [ValiderOwnership("id")]
    public async Task<IActionResult> Modifier(Guid id, PutErabliere erabliere, CancellationToken token)
    {
        if (id != erabliere.Id)
        {
            return BadRequest($"L'id de la route ne concorde pas avec l'id de l'érablière à modifier.");
        }

        var entity = await _context.Erabliere.FindAsync([id], token);

        if (entity == null)
        {
            return NotFound($"L'érablière que vous tentez de modifier n'existe pas.");
        }

        if (!string.IsNullOrWhiteSpace(erabliere.Nom) && await _context.Erabliere.AnyAsync(e => e.Id != id && e.Nom == erabliere.Nom))
        {
            return BadRequest($"L'érablière avec le nom {erabliere.Nom}");
        }

        // fin des validations

        UpdateErabliereProperties(erabliere, entity);

        _context.Erabliere.Update(entity);

        await _context.SaveChangesAsync(token);

        await _cache.RemoveAsync($"Erabliere_{id}", token);

        return Ok();
    }

    private static void UpdateErabliereProperties(PutErabliere erabliere, Erabliere entity)
    {
        if (!string.IsNullOrWhiteSpace(erabliere.Nom))
        {
            entity.Nom = erabliere.Nom;
        }

        if (!string.IsNullOrWhiteSpace(erabliere.IpRule))
        {
            entity.IpRule = erabliere.IpRule;
        }

        if (erabliere.IndiceOrdre.HasValue)
        {
            entity.IndiceOrdre = erabliere.IndiceOrdre;
        }

        if (erabliere.CodePostal != null)
        {
            entity.CodePostal = erabliere.CodePostal;
        }

        if (erabliere.AfficherSectionBaril.HasValue)
        {
            entity.AfficherSectionBaril = erabliere.AfficherSectionBaril;
        }

        if (erabliere.AfficherSectionDompeux.HasValue)
        {
            entity.AfficherSectionDompeux = erabliere.AfficherSectionDompeux;
        }

        if (erabliere.AfficherTrioDonnees.HasValue)
        {
            entity.AfficherTrioDonnees = erabliere.AfficherTrioDonnees;
        }

        if (erabliere.IsPublic.HasValue)
        {
            entity.IsPublic = erabliere.IsPublic.Value;
        }

        if (erabliere.AfficherPredictionMeteoJour.HasValue)
        {
            entity.AfficherPredictionMeteoJour = erabliere.AfficherPredictionMeteoJour;
        }

        if (erabliere.AfficherPredictionMeteoHeure.HasValue)
        {
            entity.AfficherPredictionMeteoHeure = erabliere.AfficherPredictionMeteoHeure;
        }

        if (erabliere.DimensionPanneauImage.HasValue)
        {
            entity.DimensionPanneauImage = (byte)erabliere.DimensionPanneauImage;
        }

        if (erabliere.Longitude.HasValue)
        {
            entity.Longitude = erabliere.Longitude.Value;
        }

        if (erabliere.Latitude.HasValue)
        {
            entity.Latitude = erabliere.Latitude.Value;
        }
    }

    /// <summary>
    /// Supprimer une érablière
    /// </summary>
    /// <param name="id">L'identifiant de l'érablière</param>
    /// <param name="erabliere">L'érablière a supprimer</param>
    /// <param name="token">Un jeton d'annulation</param>
    [HttpDelete("{id}")]
    [ValiderIPRules]
    [ValiderOwnership("id")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Supprimer(Guid id, DeleteErabliere<Guid> erabliere, CancellationToken token)
    {
        if (id != erabliere.Id)
        {
            return BadRequest("L'id de la route ne concorde pas avec l'id de la donnée");
        }

        var entity = await _context.Erabliere.FindAsync([erabliere.Id], token);

        if (entity != null)
        {
            var customerErabliere = await _context.CustomerErablieres
                .Where(c => c.IdErabliere == id)
                .ToArrayAsync(token);

            _context.CustomerErablieres.RemoveRange(customerErabliere);

            _context.Remove(entity);

            await _context.SaveChangesAsync(token);

            await _cache.RemoveAsync($"Erabliere_{id}", token);
        }

        return NoContent();
    }

    /// <summary>
    /// Obtenir les accès des utilisateurs à une érablière
    /// </summary>
    /// <response code="200">Les droits d'accès de l'érablère demandé</response>
    /// <response code="404">L'érablière demandée n'existe pas</response>
    [HttpGet("{id}/[action]")]
    [ValiderIPRules]
    [ValiderOwnership("id")]
    [ProducesResponseType(200, Type = typeof(GetCustomerAccess))]
    public async Task<IActionResult> Access(Guid id, CancellationToken token)
    {
        var erabliere = await _context.Erabliere.FindAsync([id], cancellationToken: token);

        if (erabliere == null)
        {
            return NotFound();
        }

        var customers = await _context.CustomerErablieres.AsNoTracking()
            .Where(c => c.IdErabliere == id)
            .ProjectTo<GetCustomerAccess>(_mapper.ConfigurationProvider)
            .ToArrayAsync(token);

        return Ok(customers);
    }

    /// <summary>
    /// Action permettant de creer les droits d'accès d'un utilisateur 
    /// à une érablière.
    /// </summary>
    /// <param name="id">L'id de l'érablière</param>
    /// <param name="idCustomer">L'id du client</param>
    /// <param name="access">Les informations sur les droits d'accès</param>
    /// <param name="token">Le token d'annulation</param>
    /// <response code="200">Les droits d'accès ont été correctement modifié.</response>
    /// <response code="400">Une des validations des paramètres à échoué.</response>
    /// <response code="404">L'érablière ou le client n'existe pas.</response>
    [HttpPost("{id}/Customer/{idCustomer}/Access")]
    [ValiderIPRules]
    [ValiderOwnership("id")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> AjouterAccess(Guid id, Guid idCustomer, PostAccess access, CancellationToken token)
    {
        var erabliere = await _context.Erabliere.FindAsync([id], token);

        if (erabliere == null)
        {
            return NotFound("L'érablière n'existe pas");
        }

        var customer = await _context.Customers.FindAsync([idCustomer], token);

        if (customer == null)
        {
            return NotFound("Le client n'existe pas");
        }

        if (!access.Access.HasValue)
        {
            return BadRequest(_localizer[ACCESS_NOT_EMPTY]);
        }

        if (access.Access.Value < 0 || access.Access.Value > 15)
        {
            return BadRequest(_localizer[ACCESS_MUST_BE_IN_0_TO_5]);
        }

        if (await _context.CustomerErablieres.AnyAsync(
            c => c.IdCustomer == idCustomer && c.IdErabliere == id, token))
        {
            return BadRequest($"L'utilisateur avec l'id {idCustomer} a déjà un droit d'accès à l'érablière avec l'id {id}.");
        }

        await _context.CustomerErablieres.AddAsync(new CustomerErabliere
        {
            Access = access.Access.Value,
            IdCustomer = idCustomer,
            IdErabliere = id
        }, token);

        await _context.SaveChangesAsync(token);

        return Ok();
    }

    /// <summary>
    /// Action permettant de creer les droits d'accès d'un utilisateur 
    /// à une érablière.
    /// </summary>
    /// <param name="id">L'id de l'érablière</param>
    /// <param name="idCustomer">L'id du client</param>
    /// <param name="access">Les informations sur les droits d'accès</param>
    /// <param name="token">Le token d'annulation</param>
    /// <response code="200">Les droits d'accès ont été correctement modifié.</response>
    /// <response code="400">Une des validations des paramètres à échoué.</response>
    /// <response code="404">L'accès n'existe pas.</response>
    [HttpPut("{id}/Customer/{idCustomer}/Access")]
    [ValiderIPRules]
    [ValiderOwnership("id")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ModifierAccess(Guid id, Guid idCustomer, PutAccess access, CancellationToken token)
    {
        if (!access.Access.HasValue)
        {
            return BadRequest(_localizer[ACCESS_NOT_EMPTY]);
        }

        if (access.Access.Value < 0 || access.Access.Value > 15)
        {
            return BadRequest(_localizer[ACCESS_MUST_BE_IN_0_TO_5]);
        }


        var entity = await _context.CustomerErablieres.FindAsync([idCustomer, id], token);

        if (entity == null)
        {
            return NotFound();
        }

        entity.Access = access.Access.Value;
        _context.CustomerErablieres.Update(entity);

        await _context.SaveChangesAsync(token);

        return Ok();
    }

    /// <summary>
    /// Supprimer les droits d'accès d'un utilisateur à une érablière
    /// </summary>
    /// <param name="id">L'identifiant de l'érablière</param>
    /// <param name="idCustomer">L'id du client</param>
    /// <param name="token">Le token d'annulation</param>
    [HttpDelete("{id}/Customer/{idCustomer}/Access")]
    [ValiderIPRules]
    [ValiderOwnership("id")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> SupprimerCustomerErabliere(Guid id, Guid idCustomer, CancellationToken token)
    {
        var entity = await _context.CustomerErablieres.FindAsync([idCustomer, id], token);

        if (entity != null && entity.IdErabliere == id)
        {
            // Valider que l'utilisateur n'est pas en train de supprimer son propre droit d'accès
            var authInfo = await IsAuthenticatedAsync(token);

            if (entity.IdCustomer != authInfo.Item3?.Id)
            {
                // Get the unique name of the user to delete
                var userToDelete = await _context.Customers.FindAsync([entity.IdCustomer], token);

                if (userToDelete != null)
                {
                    await _cache.RemoveAsync($"CustomerWithAccess_{userToDelete.UniqueName}_{id}", token);
                }

                _context.Remove(entity);

                await _context.SaveChangesAsync(token);
            }
            else
            {
                return BadRequest("Vous ne pouvez pas supprimer votre propre droit d'accès.");
            }
        }

        return NoContent();
    }

    /// <summary>
    /// Point de terminaison pour l'administration des érablières
    /// </summary>
    /// <returns>Une liste d'érablières</returns>
    /// <response code="200">Les érablières ont été correctement récupérées</response>
    [HttpGet]
    [EnableQuery]
    [Route("/Admin/Erablieres")]
    [Authorize(Roles = "administrateur", Policy = "TenantIdPrincipal")]
    public IQueryable<Erabliere> GetErablieresAdmin()
    {
        return _context.Erabliere;
    }

    /// <summary>
    /// Modifier une érablière en tant qu'administrateur
    /// </summary>
    /// <param name="id">L'identifiant de l'érablière</param>
    /// <param name="erabliere">Les données de l'érablière à modifier.
    ///     1. L'id doit concorder avec celui de la route.
    ///     2. L'érablière doit exister.
    ///     3. Si le nom est modifié, il ne doit pas être pris par une autre érablière.</param>
    /// <param name="token">Un jeton d'annulation</param>
    /// <response code="200">L'érablière a été correctement modifiée</response>
    /// <response code="400">Une des validations des paramètres a échoué.</response>
    /// <response code="404">L'érablière n'a pas été trouvée</response>
    [HttpPut]
    [Route("/Admin/Erablieres/{id}")]
    [Authorize(Roles = "administrateur", Policy = "TenantIdPrincipal")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ModifierAdmin(Guid id, PutAdminErabliere erabliere, CancellationToken token)
    {
        if (id != erabliere.Id)
        {
            return BadRequest($"L'id de la route ne concorde pas avec l'id de l'érablière à modifier.");
        }

        var entity = await _context.Erabliere.FindAsync([id], token);

        if (entity == null)
        {
            return NotFound($"L'érablière que vous tentez de modifier n'existe pas.");
        }

        if (!string.IsNullOrWhiteSpace(erabliere.Nom) && await _context.Erabliere.AnyAsync(e => e.Id != id && e.Nom == erabliere.Nom, token))
        {
            return BadRequest($"L'érablière avec le nom {erabliere.Nom}");
        }

        // fin des validations

        if (!string.IsNullOrWhiteSpace(erabliere.Nom))
        {
            entity.Nom = erabliere.Nom;
        }

        if (!string.IsNullOrWhiteSpace(erabliere.IpRule))
        {
            entity.IpRule = erabliere.IpRule;
        }

        if (erabliere.IndiceOrdre.HasValue)
        {
            entity.IndiceOrdre = erabliere.IndiceOrdre;
        }

        if (erabliere.CodePostal != null)
        {
            entity.CodePostal = erabliere.CodePostal;
        }

        if (erabliere.AfficherSectionBaril.HasValue)
        {
            entity.AfficherSectionBaril = erabliere.AfficherSectionBaril;
        }

        if (erabliere.AfficherSectionDompeux.HasValue)
        {
            entity.AfficherSectionDompeux = erabliere.AfficherSectionDompeux;
        }

        if (erabliere.AfficherTrioDonnees.HasValue)
        {
            entity.AfficherTrioDonnees = erabliere.AfficherTrioDonnees;
        }

        if (erabliere.IsPublic.HasValue)
        {
            entity.IsPublic = erabliere.IsPublic.Value;
        }

        if (erabliere.DC.HasValue)
        {
            entity.DC = erabliere.DC.Value;
        }

        _context.Erabliere.Update(entity);

        await _context.SaveChangesAsync(token);

        await _cache.RemoveAsync($"Erabliere_{id}", token);

        return Ok();
    }

    /// <summary>
    /// Supprimer une érablière en tant qu'administrateur
    /// </summary>
    /// <param name="id">L'identifiant de l'érablière</param>
    /// <param name="token">Un jeton d'annulation</param>
    /// <response code="204">L'érablière a été correctement supprimée</response>
    /// <response code="404">L'érablière n'a pas été trouvée</response>
    [HttpDelete]
    [Route("/Admin/Erablieres/{id}")]
    [Authorize(Roles = "administrateur", Policy = "TenantIdPrincipal")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteErablieresAdmin(Guid id, CancellationToken token)
    {
        var entity = await _context.Erabliere.Include(e => e.CustomerErablieres).FirstOrDefaultAsync(e => e.Id == id, token);

        if (entity != null)
        {
            _context.Remove(entity);

            await _context.SaveChangesAsync(token);

            await _cache.RemoveAsync($"Erabliere_{id}", token);

            return NoContent();
        }

        return NotFound();
    }

    /// <summary>
    /// Obtenir les accès des utilisateurs à une érablière en tant qu'administrateur
    /// </summary>
    /// <response code="200">Les droits d'accès de l'érablère demandé</response>
    /// <response code="404">L'érablière demandée n'existe pas</response>
    [HttpGet("/Admin/Erablieres/{id}/Access")]
    [Authorize(Roles = "administrateur", Policy = "TenantIdPrincipal")]
    [ProducesResponseType(200, Type = typeof(GetCustomerAccess[]))]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetAdminAccess(Guid id, CancellationToken token)
    {
        var erabliere = await _context.Erabliere.FindAsync([id], cancellationToken: token);

        if (erabliere == null)
        {
            return NotFound();
        }

        var customers = await _context.CustomerErablieres.AsNoTracking()
            .Where(c => c.IdErabliere == id)
            .ProjectTo<GetCustomerAccess>(_mapper.ConfigurationProvider)
            .ToArrayAsync(token);

        return Ok(customers);
    }

    /// <summary>
    /// Action permettant de creer les droits d'accès d'un utilisateur en tant qu'administrateur
    /// à une érablière.
    /// </summary>
    /// <param name="id">L'id de l'érablière</param>
    /// <param name="idCustomer">L'id du client</param>
    /// <param name="access">Les informations sur les droits d'accès</param>
    /// <param name="token">Le token d'annulation</param>
    /// <response code="200">Les droits d'accès ont été correctement modifié.</response>
    /// <response code="400">Une des validations des paramètres à échoué.</response>
    /// <response code="404">L'érablière ou le client n'existe pas.</response>
    [HttpPost("/Admin/Erablieres/{id}/Customer/{idCustomer}/Access")]
    [Authorize(Roles = "administrateur", Policy = "TenantIdPrincipal")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> AjouterAdminAccess(Guid id, Guid idCustomer, PostAccess access, CancellationToken token)
    {
        var erabliere = await _context.Erabliere.FindAsync([id], token);

        if (erabliere == null)
        {
            return NotFound("L'érablière n'existe pas");
        }

        var customer = await _context.Customers.FindAsync([idCustomer], token);

        if (customer == null)
        {
            return NotFound("Le client n'existe pas");
        }

        if (!access.Access.HasValue)
        {
            return BadRequest(_localizer[ACCESS_NOT_EMPTY]);
        }

        if (access.Access.Value < 0 || access.Access.Value > 15)
        {
            return BadRequest(_localizer[ACCESS_MUST_BE_IN_0_TO_5]);
        }


        if (await _context.CustomerErablieres.AnyAsync(
            c => c.IdCustomer == idCustomer && c.IdErabliere == id, token))
        {
            return BadRequest($"L'utilisateur avec l'id {idCustomer} a déjà un droit d'accès à l'érablière avec l'id {id}.");
        }

        await _context.CustomerErablieres.AddAsync(new CustomerErabliere
        {
            Access = access.Access.Value,
            IdCustomer = idCustomer,
            IdErabliere = id
        }, token);

        await _context.SaveChangesAsync(token);

        return Ok();
    }

    /// <summary>
    /// Action permettant de creer les droits d'accès d'un utilisateur en tant qu'administrateur
    /// à une érablière.
    /// </summary>
    /// <param name="id">L'id de l'érablière</param>
    /// <param name="idCustomer">L'id du client</param>
    /// <param name="access">Les informations sur les droits d'accès</param>
    /// <param name="token">Le token d'annulation</param>
    /// <response code="200">Les droits d'accès ont été correctement modifié.</response>
    /// <response code="400">Une des validations des paramètres à échoué.</response>
    /// <response code="404">L'accès n'existe pas.</response>
    [HttpPut("/Admin/Erablieres/{id}/Customer/{idCustomer}/Access")]
    [Authorize(Roles = "administrateur", Policy = "TenantIdPrincipal")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ModifierAdminAccess(Guid id, Guid idCustomer, PutAccess access, CancellationToken token)
    {
        if (!access.Access.HasValue)
        {
            return BadRequest(_localizer[ACCESS_NOT_EMPTY]);
        }

        if (access.Access.Value < 0 || access.Access.Value > 15)
        {
            return BadRequest(_localizer[ACCESS_MUST_BE_IN_0_TO_5]);
        }


        var entity = await _context.CustomerErablieres.FindAsync([idCustomer, id], token);
        if (entity == null)
        {
            return NotFound();
        }

        entity.Access = access.Access.Value;
        _context.CustomerErablieres.Update(entity);

        await _context.SaveChangesAsync(token);

        return Ok();
    }

    /// <summary>
    /// Supprimer les droits d'accès d'un utilisateur à une érablière en tant qu'administrateur
    /// </summary>
    /// <param name="id">L'identifiant de l'érablière</param>
    /// <param name="idCustomer">L'id du client</param>
    /// <param name="token">Le token d'annulation</param>
    [HttpDelete("/Admin/Erablieres/{id}/Customer/{idCustomer}/Access")]
    [Authorize(Roles = "administrateur", Policy = "TenantIdPrincipal")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> SupprimerAdminAccess(Guid id, Guid idCustomer, CancellationToken token)
    {
        var entity = await _context.CustomerErablieres.FindAsync([idCustomer, id], token);

        if (entity == null)
        {
            return NotFound();
        }

        // Get the unique name of the user to delete
        var userToDelete = await _context.Customers.FindAsync([idCustomer], token);

        if (userToDelete != null)
        {
            await _cache.RemoveAsync($"CustomerWithAccess_{userToDelete.UniqueName}_{id}", token);
        }

        _context.Remove(entity);

        await _context.SaveChangesAsync(token);

        return NoContent();
    }
}
