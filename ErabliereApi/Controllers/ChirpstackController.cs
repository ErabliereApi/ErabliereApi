using ErabliereApi.Controllers.Base;
using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Donnees.Action.Post;
using ErabliereApi.Extensions;
using ErabliereApi.Services.LoRaWAN;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Text.Json;

namespace ErabliereApi.Controllers;

/// <summary>
/// Contrôler de réception des événements et des configurations de Chirpstack
/// </summary>
[ApiController]
[Route("/[controller]")]
[Authorize]
public class ChirpstackController : ErabliereApiBaseController
{
    private readonly ErabliereDbContext _context;
    private readonly IConfiguration _config;
    private readonly ILogger<ChirpstackController> _logger;

    /// <summary>
    /// Constructeur
    /// </summary>
    /// <param name="context"></param>
    /// <param name="config"></param>
    /// <param name="serviceProvider"></param>
    public ChirpstackController(
        ErabliereDbContext context, IConfiguration config, IServiceProvider serviceProvider, ILogger<ChirpstackController> logger)
         : base(serviceProvider, context, config)
    {
        _context = context;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Permet de tester le decoder sur un paquet LoRaWAN en base64
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    [HttpGet("decode")]
    public IActionResult Decode([FromQuery] string data)
    {
        var (array, crc) = LoRaWANPacketDecoder.TryDecodeData(data, _logger);

        return Ok(new
        {
            values = array,
            crc
        });
    }

    /// <summary>
    /// Récepteur de l'intégration Http disponible dans Chirpstack
    /// </summary>
    /// <param name="eventStr"></param>
    /// <param name="eventInfo"></param>
    /// <param name="token"></param>
    /// <returns></returns>
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
        else
        {
            srvInfo.LastTimeSeen = DateTimeOffset.Now;
            srvInfo.LastDeviceSeen = eventInfo.deviceInfo.devEui;
            await _context.TrySaveChangesAsync(token);
        }

        await ManageHistory(srvInfo, eventInfo, token);

        var idErabliere = eventInfo.deviceInfo.tags.idErabliere;

        if (idErabliere == null)
        {
            var message = "L'id de l'érablière est requis";

            ModelState.AddModelError("deviceInfo.tags.idErabliere", message);

            _logger.LogWarning("Chirpstack event listner return bad request with message: {Message}", message);

            return BadRequest(new ValidationProblemDetails(ModelState));
        }

        // Mapper la données vers les bons capteurs
        var capteurs = await _context.Capteurs
            .Where(c => c.IdErabliere == idErabliere &&
                        c.ExternalId == eventInfo.deviceInfo.devEui)
            .ToArrayAsync(token);

        if (capteurs.Length == 0)
        {
            var message = "Aucun capteur assossié avec l'érablière ou l'id d'appareil (ExternalId - devEui).";

            _logger.LogWarning("Chirpstack event listner return bad request with message: {Message}", message);

            return BadRequest(message);
        }

        // Vérifier l'autorité de l'appelant
        if (_config.IsAuthEnabled())
        {
            var (a, b, customer) = await IsAuthenticatedAsync(token);

            if (customer == null)
            {
                var message = "Customer not found";

                _logger.LogWarning("Chirpstack event listner return Unauthorize with message: {Message}", message);

                return Unauthorized(message);
            }

            var access = await _context.CustomerErablieres
                .Where(ce => ce.IdErabliere == idErabliere &&
                             ce.IdCustomer == customer.Id)
                .FirstOrDefaultAsync(token);

            if (access == null)
            {
                var message = "No access";

                _logger.LogWarning("Chirpstack event listner return Unauthorize with message: {Message}", message);

                return Unauthorized(message);
            }

            if ((access.Access & 2) == 0)
            {
                var message = "No create access";

                _logger.LogWarning("Chirpstack event listner return Unauthorize with message: {Message}", message);

                return Unauthorized(message);
            }
        }

        var (decodedData, crc) = LoRaWANPacketDecoder.TryDecodeData(eventInfo.data, _logger);

        // Enregistrer en BD
        foreach (var d in decodedData)
        {
            var cas = capteurs.Where(c => c.IdMesure == d.Mesure);
            Capteur? ca = cas.FirstOrDefault();

            if (cas.Count() > 1)
            {
                _logger.LogWarning("More than one sensor is mathing the mesurement idMesure {IdMesure}", d.Mesure);
            }

            if (ca != null)
            {
                ca.Online = true;
                ca.LastMessageTime = DateTimeOffset.Now;

                await _context.DonneesCapteur.AddAsync(new DonneeCapteur
                {
                    IdCapteur = ca.Id,
                    Valeur = d.Value,
                    D = DateTimeOffset.Now
                });
            }
            else
            {
                _logger.LogWarning("No sensor is matching the mesurment idMesure {IdMesure}", d.Mesure);
            }
        }

        await _context.SaveChangesAsync(token);

        return Ok();
    }

    private async Task ManageHistory(ChirpStackSrvConfig server, PostChirpstackEvent eventInfo, CancellationToken token)
    {
        var history = await _context.ChirpstackMessageHistory
            .Where(h => h.ChirpStackSrvConfigId == server.Id)
            .OrderByDescending(h => h.Date)
            .ToListAsync(token);

        var limit = DateTimeOffset.Now - server.TimeToKeepLastMessage;

        for (int i = 0; i < history.Count; i++)
        {
            ChirpStackMessage? h = history[i];
            if (h.Date < limit)
            {
                _context.ChirpstackMessageHistory.Remove(h);
                history.RemoveAt(i--);
            }
        }

        if (history.Count > server.KeepNLastMessage)
        {
            var delta = history.Count - server.KeepNLastMessage;

            var toDelete = history.Take(delta);

            _context.ChirpstackMessageHistory.RemoveRange(toDelete);
        }

        await _context.ChirpstackMessageHistory.AddAsync(new ChirpStackMessage
        {
            ChirpStackSrvConfigId = server.Id,
            Data = eventInfo.data,
            Date = DateTimeOffset.Now,
            DecodedData = JsonSerializer.Serialize(LoRaWANPacketDecoder.TryDecodeData(eventInfo.data, _logger)),
            MessageJson = JsonSerializer.Serialize(eventInfo)
        }, token);

        await _context.TrySaveChangesAsync(token);
    }

    /// <summary>
    /// Lister les serveur Chirpstack configuré
    /// </summary>
    /// <returns></returns>
    [HttpGet("configs")]
    [EnableQuery]
    [Authorize(Roles = "administrateur", Policy = "TenantIdPrincipal")]
    public IQueryable<ChirpStackSrvConfig> GetConfigs()
    {
        return _context.ChirpstackSrvConfigs.AsNoTracking();
    }

    /// <summary>
    /// Ajouté un serveur Chirpstack autorisé
    /// </summary>
    /// <param name="chirpStackSrvConfig"></param>
    /// <param name="token"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Modifier un serveur Chirpstack
    /// </summary>
    /// <param name="id"></param>
    /// <param name="chirpStackSrvConfig"></param>
    /// <param name="token"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Supprimer un serveur Chirpstack
    /// </summary>
    /// <param name="id"></param>
    /// <param name="token"></param>
    /// <returns></returns>
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
