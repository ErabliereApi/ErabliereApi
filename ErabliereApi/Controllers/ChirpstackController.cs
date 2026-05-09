using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Donnees.Action.Post;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ErabliereApi.Controllers;

[ApiController]
[Route("/[controller]")]
[Authorize]
public class ChirpstackController : ControllerBase
{
    private readonly ErabliereDbContext _context;

    public ChirpstackController(ErabliereDbContext context)
    {
        _context = context;
    }

    [HttpPost("events")]
    public async Task<IActionResult> EventListener(
        [FromQuery(Name = "event")] string? eventStr, [FromBody] PostChirpstackEvent eventInfo, CancellationToken token)
    {
        if (eventInfo == null)
        {
            ModelState.AddModelError("request body", "The event is null");

            return BadRequest(new ValidationProblemDetails(ModelState));
        }

        if (eventInfo.deviceInfo == null)
        {
            ModelState.AddModelError("deviceInfo", "The result.deviceInfo property is null");

            return BadRequest(new ValidationProblemDetails(ModelState));
        }

        // Vérifier l'appareil distant depuis la configuration
        var srvInfo = await _context.ChirpstackSrvConfigs
            .Where(c => c.TenantId == eventInfo.deviceInfo.tenantId &&
                        c.ApplicationId == eventInfo.deviceInfo.applicationId &&
                        c.DeviceProfileId == eventInfo.deviceInfo.deviceProfileId)
            .FirstOrDefaultAsync(token);

        if (srvInfo == null)
        {
            return BadRequest("No device info matching the server sending the request");
        }

        var idErabliere = eventInfo.deviceInfo.tags.idErabliere;

        if (idErabliere == null)
        {
            ModelState.AddModelError("deviceInfo.tags.idErabliere", "L'id de l'érablière est requis");

            return BadRequest(new ValidationProblemDetails(ModelState));
        }

        // Mapper la données vers les bons capteurs
        var capteurs = await _context.Capteurs
            .Where(c => c.IdErabliere == idErabliere &&
                        c.ExternalId == eventInfo.deviceInfo.devEui)
            .ToArrayAsync(token);

        if (capteurs.Length == 0)
        {
            return BadRequest("Aucun capteur assossié avec l'érablière ou l'id d'appareil (ExternalId - devEui).");
        }

        // Vérifier l'autorité de la clé d'API

        // Décoder les données
        var data = eventInfo.data;

        // Enregistrer en BD
        await _context.SaveChangesAsync(token);

        return Ok();
    }

    [HttpGet("configs")]
    [EnableQuery]
    [Authorize(Roles = "administrateur", Policy = "TenantIdPrincipal")]
    public IQueryable<ChirpStackSrvConfig> GetConfigs()
    {
        return _context.ChirpstackSrvConfigs.AsNoTracking();
    }

    [HttpPost("configs")]
    [Authorize(Roles = "administrateur", Policy = "TenantIdPrincipal")]
    [ProducesDefaultResponseType(typeof(ChirpStackSrvConfig))]
    public async Task<IActionResult> CreateConfigs(
        [FromBody] ChirpStackSrvConfig chirpStackSrvConfig, CancellationToken token)
    {
        var result = await _context.AddAsync(chirpStackSrvConfig, token);

        await _context.SaveChangesAsync(token);

        return Ok(result.Entity);
    }

    [HttpPut("configs/{id}")]
    [Authorize(Roles = "administrateur", Policy = "TenantIdPrincipal")]
    [ProducesDefaultResponseType(typeof(ChirpStackSrvConfig))]
    public async Task<IActionResult> EditConfigs([FromRoute] Guid id,
        [FromBody] ChirpStackSrvConfig chirpStackSrvConfig, CancellationToken token)
    {
        _context.Update(chirpStackSrvConfig);

        await _context.SaveChangesAsync(token);

        return NoContent();
    }

    [HttpDelete("configs/{id}")]
    [Authorize(Roles = "administrateur", Policy = "TenantIdPrincipal")]
    [ProducesDefaultResponseType(typeof(ChirpStackSrvConfig))]
    public async Task<IActionResult> DeleteConfigs([FromRoute] Guid id, CancellationToken token)
    {
        var config = await _context.ChirpstackSrvConfigs.FindAsync([id], token);

        if (config != null)
        {
            _context.Remove(config);

            await _context.SaveChangesAsync(token);
        }

        return NoContent();
    }
}