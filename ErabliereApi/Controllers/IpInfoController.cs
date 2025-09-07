using ErabliereApi.Depot.Sql;
using ErabliereApi.Extensions;
using ErabliereApi.Middlewares;
using ErabliereApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ErabliereApi.Controllers;

/// <summary>
/// Contrôleur pour gérer les informations IP
/// </summary>
[ApiController]
[Route("ipinfo")]
[Authorize(Roles = "administrateur", Policy = "TenantIdPrincipal")]
public class IpInfoController : ControllerBase
{
    private readonly ErabliereDbContext _context;
    private readonly IMemoryCache _memoryCache;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Constructeur
    /// </summary>
    public IpInfoController(ErabliereDbContext context, IMemoryCache memoryCache, IConfiguration configuration)
    {
        _context = context;
        _memoryCache = memoryCache;
        _configuration = configuration;
    }

    /// <summary>
    /// Récupère les informations IP stockées dans la base de données
    /// </summary>
    /// <returns>Liste des informations IP</returns>
    [HttpGet]
    [EnableQuery]
    public IQueryable<IpInfo> GetIpInfo()
    {
        return _context.IpInfos.AsNoTracking();
    }

    /// <summary>
    /// Récupère les informations ASN des réseaux IP stockées dans la base de données
    /// </summary>
    [HttpGet("asn/{asn}")]
    public IQueryable<IpNetworkAsnInfo> GetIpNetworkAsnInfo(string asn)
    {
        return _context.IpNetworkAsnInfos.AsNoTracking().Where(info => info.ASN == asn);
    }

    /// <summary>
    /// Récupère la liste des pays autorisés à accéder à l'API
    /// </summary>
    /// <returns>Liste des codes de pays autorisés</returns>
    [HttpGet("authorized-countries")]
    public IEnumerable<string> GetAuthorizedCountries()
    {
        return _configuration.GetSection("IpInfoApi:AuthorizeCountries").Get<List<string>>() ?? [];
    }

    /// <summary>
    /// Importe une liste d'informations IP dans la base de données
    /// </summary>
    /// <param name="ipInfos">Liste des informations IP à importer</param>
    /// <param name="cancellationToken">Jeton d'annulation pour la requête</param>
    /// <returns>Résultat de l'opération</returns>
    [HttpPost("import")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ImportIpInfo([FromBody] List<IpInfo> ipInfos, CancellationToken cancellationToken)
    {
        if (ipInfos == null || !ipInfos.Any())
        {
            return BadRequest("Invalid IP information.");
        }

        await _context.IpInfos.AddRangeAsync(ipInfos, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Bloque une adresse IP en mettant à jour son statut dans la base de données
    /// </summary>
    /// <param name="id">Identifiant unique de l'information IP à bloquer</param>
    /// <param name="operation">Opération à effectuer : "block" pour bloquer, "allow" pour autoriser</param>
    /// <param name="cancellationToken">Jeton d'annulation pour la requête</param>
    /// <returns>Résultat de l'opération</returns>
    [HttpPut("{id:guid}/{operation}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BlockIp(Guid id, string operation, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            ModelState.AddModelError(nameof(id), "L'identifiant ne peut pas être vide");

            return BadRequest(new ValidationProblemDetails(ModelState));
        }

        if (operation != "block" && operation != "allow")
        {
            ModelState.AddModelError(nameof(operation), "L'opération doit être 'block' ou 'allow'");

            return BadRequest(new ValidationProblemDetails(ModelState));
        }

        var ipInfo = await _context.IpInfos.FindAsync([id], cancellationToken);

        if (ipInfo == null)
        {
            return NotFound();
        }

        ipInfo.IsAllowed = operation == "allow";
        ipInfo.DM = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _memoryCache.Set(
            $"{IpInfoMiddleware.CacheKeyPrefix}{ipInfo.Ip}",
            ipInfo,
            _configuration.GetRequiredValue<TimeSpan>("IpInfoApi:CacheDuration"));

        return NoContent();
    }

    /// <summary>
    /// Supprime une information IP de la base de données
    /// </summary>
    /// <param name="id">Identifiant unique de l'information IP à supprimer</param>
    /// <param name="cancellationToken">Jeton d'annulation pour la requête</param>
    /// <returns>Résultat de l'opération</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteIp(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            ModelState.AddModelError(nameof(id), "L'identifiant ne peut pas être vide");

            return BadRequest(new ValidationProblemDetails(ModelState));
        }

        var ipInfo = await _context.IpInfos.FindAsync([id], cancellationToken);

        if (ipInfo == null)
        {
            return NotFound();
        }

        _context.IpInfos.Remove(ipInfo);
        await _context.SaveChangesAsync(cancellationToken);

        _memoryCache.Remove($"{IpInfoMiddleware.CacheKeyPrefix}{ipInfo.Ip}");

        return NoContent();
    }
}