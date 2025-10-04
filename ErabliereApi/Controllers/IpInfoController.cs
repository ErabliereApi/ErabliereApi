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
    private readonly ILogger<IpInfoController> _logger;

    /// <summary>
    /// Constructeur
    /// </summary>
    public IpInfoController(ErabliereDbContext context,
        IMemoryCache memoryCache,
        IConfiguration configuration,
        ILogger<IpInfoController> logger)
    {
        _context = context;
        _memoryCache = memoryCache;
        _configuration = configuration;
        _logger = logger;
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
    /// Liste les informations IP ASN
    /// </summary>
    [HttpGet("asn")]
    [EnableQuery]
    public IQueryable<IpNetworkAsnInfo> GetIpNetworkAsnInfos()
    {
        return _context.IpNetworkAsnInfos.AsNoTracking();
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
    /// Récupère les nombre d'IP par pays
    /// </summary>
    /// <returns></returns>
    [HttpGet("group-by-country")]
    public IQueryable<object?> GetIpGroupByCountry()
    {
        return _context.IpInfos.AsNoTracking()
            .GroupBy(info => new { info.Country, info.CountryCode })
            .Select(g => new
            {
                g.Key.Country,
                g.Key.CountryCode,
                Count = g.Count()
            });
    }

    /// <summary>
    /// Récupère la liste des pays autorisés à accéder à l'API
    /// </summary>
    /// <returns>Liste des codes de pays autorisés</returns>
    [HttpGet("authorized-countries")]
    public IEnumerable<string> GetAuthorizedCountries()
    {
        return _configuration.GetSection("IpInfoApi:AuthorizeCountries")
            .Get<List<string>>() ?? [];
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
    /// Une action permettant d'envoyer un fichier xlsx pour importer des informations IP Réseau et ASN dans la BD.
    /// </summary>
    /// <param name="file">Le fichier xlsx contenant les informations à importer</param>
    /// <param name="cancellationToken">Jeton d'annulation pour la requête</param>
    /// <returns>Résultat de l'opération</returns>
    [HttpPost("import-asn")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [RequestSizeLimit(52428800)] // 50 MB in bytes
    public async Task<IActionResult> ImportIpNetworkAsnInfo(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("Le fichier est invalide.");
        }

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Le fichier doit être au format xlsx.");
        }

        await Console.Out.WriteLineAsync($"{DateTimeOffset.UtcNow} Début de l'importation des informations IP ASN...");
        using var workbook = new ClosedXML.Excel.XLWorkbook(file.OpenReadStream());
        await Console.Out.WriteLineAsync($"{DateTimeOffset.UtcNow} Fichier chargé en mémoire.");
        var worksheet = workbook.Worksheets.First();
        await Console.Out.WriteLineAsync($"{DateTimeOffset.UtcNow} Feuille de calcul sélectionnée : {worksheet.Name}");
        var bufferSize = 15000;
        var buffer = new List<IpNetworkAsnInfo>(bufferSize);
        var totalSaved = 0;

        var loopHasBegun = false;

        await Console.Out.WriteLineAsync($"{DateTimeOffset.UtcNow} Début du parcourt des lignes network IP ASN...");
        foreach (var row in worksheet.Rows().Skip(1))
        {
            if (!loopHasBegun)
            {
                loopHasBegun = true;
                await Console.Out.WriteLineAsync($"{DateTimeOffset.UtcNow} Parcourt des lignes commencé...");
            }

            var cells = row.Cells().ToArray();

            if (cells.Length <= 2)
            {
                _logger.LogWarning("Ligne ignorée en raison d'un nombre insuffisant de colonnes : {RowNumber}", row.RowNumber());
                continue;
            }

            var ipNetworkAsnInfo = new IpNetworkAsnInfo
            {
                Network = cells.AtIndexOrDefault(0)?.GetString() ?? string.Empty,
                Country = cells.AtIndexOrDefault(1)?.GetString() ?? string.Empty,
                CountryCode = cells.AtIndexOrDefault(2)?.GetString() ?? string.Empty,
                Continent = cells.AtIndexOrDefault(3)?.GetString() ?? string.Empty,
                ContinentCode = cells.AtIndexOrDefault(4)?.GetString() ?? string.Empty,
                ASN = cells.AtIndexOrDefault(5)?.GetString() ?? string.Empty,
                AS_Name = (cells.AtIndexOrDefault(6)?.GetString() ?? string.Empty).Trim(),
                AS_Domain = cells.AtIndexOrDefault(7)?.GetString() ?? string.Empty,
            };

            if (ipNetworkAsnInfo.AS_Name.Length > 200)
            {
                _logger.LogWarning("Troncature du nom AS pour le réseau {Network} car il dépasse 200 caractères. Text: {AS_Name}", ipNetworkAsnInfo.Network, ipNetworkAsnInfo.AS_Name);

                ipNetworkAsnInfo.AS_Name = ipNetworkAsnInfo.AS_Name[..200];
            }

            buffer.Add(ipNetworkAsnInfo);

            if (buffer.Count >= bufferSize)
            {
                var t = Console.Out.WriteLineAsync($"{DateTimeOffset.UtcNow} Sauvegarde de {buffer.Count} enregistrements...");
                await _context.IpNetworkAsnInfos.AddRangeAsync(buffer, cancellationToken);
                totalSaved += await _context.SaveChangesAsync(cancellationToken);
                buffer.Clear();
                await t;
            }
        }

        totalSaved += await _context.SaveChangesAsync(cancellationToken);

        await Console.Out.WriteLineAsync($"Total des enregistrements sauvegardés : {totalSaved}");

        return Ok(new { totalSaved });
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

    /// <summary>
    /// Enlève tout les informations IP ASN de la base de données
    /// </summary>
    [HttpDelete("asn")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteAllIpNetworkAsnInfo(CancellationToken cancellationToken)
    {
        _context.IpNetworkAsnInfos.RemoveRange(_context.IpNetworkAsnInfos);
        var totalDeleted = await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { totalDeleted });
    }
}