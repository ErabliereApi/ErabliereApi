using ErabliereApi.Attributes;
using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Services.Nmap;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;

namespace ErabliereApi.Controllers;

/// <summary>
/// Controller pour les appareils
/// </summary>
[ApiController]
[Route("Erablieres/{id}/[controller]")]
[Authorize]
public class AppareilController : ControllerBase
{
    private readonly ErabliereDbContext _context;
    private readonly NmapService _nmapService;

    /// <summary>
    /// Constructeur
    /// </summary>
    /// <param name="context"></param>
    /// <param name="nmapService"></param>
    public AppareilController(ErabliereDbContext context, NmapService nmapService)
    {
        _context = context;
        _nmapService = nmapService;
    }

    /// <summary>
    /// Lister les appareils de l'érablière
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet]
    [EnableQuery]
    [ValiderOwnership("id")]
    public IQueryable<Appareil> Lister(Guid id)
    {
        return _context.Appareils.AsNoTracking().Where(a => a.IdErabliere == id);
    }

    /// <summary>
    /// Mise à jour des appareils de l'érablière à partir d'un résultat de scan nmap
    /// </summary>
    /// <param name="id">Id de l'éralière</param>
    /// <param name="nmapResult">Contenue de la sortie xml de nmap ```-oX result.xml```</param>
    /// <param name="token"></param>
    /// <returns></returns>
    [HttpPut("nmapscan")]
    [ValiderOwnership("id")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> MiseAJourScanNmap(Guid id, [FromBody] string nmapResult, CancellationToken token)
    {
        var nmapObj = new System.Xml.XmlDocument();

        nmapObj.LoadXml(nmapResult);

        await _nmapService.UpdateDevicesFromNmapScanAsync(id, nmapObj, token);

        return NoContent();
    }

    /// <summary>
    /// Supprimer tout les appareils de l'érablière
    /// </summary>
    /// <param name="id"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    [HttpDelete]
    [ValiderOwnership("id")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Supprimer(Guid id, CancellationToken token)
    {
        var appareils = await _context.Appareils
            .Include(a => a.Statut)
            .Include(a => a.Adresses)
            .Include(a => a.Ports)
            .Include(a => a.NomsHost)
            .Where(a => a.IdErabliere == id).ToListAsync(token);

        if (appareils == null || appareils.Count == 0)
        {
            return NotFound();
        }

        _context.Appareils.RemoveRange(appareils);
        await _context.SaveChangesAsync(token);

        return NoContent();
    }
}
