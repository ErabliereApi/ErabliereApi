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

        // TODO: Créer les endpoints d'administration de serveur chirpstack
        // Vérifier l'appareil distant depuis la configuration
        var srvInfo = await _context.ChirpstackSrvConfigs
            .Where(c => c.DevEui == eventInfo.deviceInfo.devEui)
            .SingleOrDefaultAsync(token);

        // Mapper la données vers les bons capteurs

        // Vérifier l'autorité de la clé d'API

        // Décoder les données

        // Enregistrer en BD

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