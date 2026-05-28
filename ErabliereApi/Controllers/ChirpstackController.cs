using ErabliereApi.Controllers.Base;
using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Donnees.Action.Post;
using ErabliereApi.Extensions;
using ErabliereApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Text.Json;
using static ErabliereApi.Services.LoRaWAN.LoRaWANPacketDecoder;
using static ErabliereApi.Services.AlerteHelper;
using Microsoft.Extensions.Options;

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
    private readonly IOptions<EmailConfig> _emailConfig;
    private readonly IOptions<SMSConfig> _smsConfig;
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;

    /// <summary>
    /// Constructeur
    /// </summary>
    /// <param name="context"></param>
    /// <param name="config"></param>
    /// <param name="serviceProvider"></param>
    /// <param name="logger"></param>
    /// <param name="emailConfig"></param>
    /// <param name="smsConfig"></param>
    /// <param name="emailService"></param>
    /// <param name="smsService"></param>
    public ChirpstackController(
        ErabliereDbContext context,
        IConfiguration config,
        IServiceProvider serviceProvider,
        ILogger<ChirpstackController> logger,
        IOptions<EmailConfig> emailConfig,
        IOptions<SMSConfig> smsConfig,
        IEmailService emailService,
        ISmsService smsService)
         : base(serviceProvider, context, config)
    {
        _context = context;
        _config = config;
        _logger = logger;
        _emailConfig = emailConfig;
        _smsConfig = smsConfig;
        _emailService = emailService;
        _smsService = smsService;
    }

    /// <summary>
    /// Permet de tester le decoder sur un paquet LoRaWAN en base64
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    [HttpGet("decode")]
    [ProducesResponseType(200, Type = typeof(MesurementResponse))]
    public IActionResult Decode([FromQuery] string data)
    {
        var mesurementResponse = TryDecodeData(data, _logger);

        return Ok(mesurementResponse);
    }

    private static ConcurrentDictionary<string, SemaphoreSlim> _semaphoreSlim = new ConcurrentDictionary<string, SemaphoreSlim>();

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

            _logger.LogWarning("The request body is null, return BadRequest");

            return BadRequest(new ValidationProblemDetails(ModelState));
        }

        if (eventInfo.deviceInfo == null)
        {
            ModelState.AddModelError("deviceInfo", "The result.deviceInfo property is null");

            _logger.LogWarning("deviceInfo property is null, return BadRequest");

            return BadRequest(new ValidationProblemDetails(ModelState));
        }

        var sem = _semaphoreSlim.GetOrAdd(
            $"{eventInfo.deviceInfo.tenantId}-{eventInfo.deviceInfo.applicationId}-{eventInfo.deviceInfo.deviceProfileId}",
            (key) => new SemaphoreSlim(1));

        await sem.WaitAsync(token);

        try
        {
            // Vérifier l'appareil distant depuis la configuration
            var srvInfo = await _context.ChirpstackSrvConfigs
                .Where(c => c.TenantId == eventInfo.deviceInfo.tenantId &&
                            c.ApplicationId == eventInfo.deviceInfo.applicationId &&
                            c.DeviceProfileId == eventInfo.deviceInfo.deviceProfileId)
                .FirstOrDefaultAsync(token);

            if (srvInfo == null)
            {
                _logger.LogWarning("No device info matching the server sending the request, return BadRequest");

                return BadRequest("No device info matching the server sending the request");
            }
            else
            {
                srvInfo.LastTimeSeen = DateTimeOffset.Now;
                srvInfo.LastDeviceSeen = eventInfo.deviceInfo.devEui;
                await _context.TrySaveChangesAsync(token);
            }

            await ManageHistory(srvInfo, eventStr, eventInfo, token);

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

            var mesurementsResponse = TryDecodeData(eventInfo.data, _logger);

            if (mesurementsResponse.Mesurements != null)
            {
                // Enregistrer en BD
                foreach (var d in mesurementsResponse.Mesurements)
                {
                    if (d.Mesure == 7)
                    {
                        foreach (var c in capteurs)
                        {
                            c.BatteryLevel = (byte?)d.Value;
                        }
                        continue;
                    }
                    if (d.Mesure == 8)
                    {
                        foreach (var c in capteurs)
                        {
                            c.ReportFrequency = (int?)d.Value;
                        }
                        continue;
                    }
                    if (d.Mesure == 9 || d.Mesure == 10)
                    {
                        _logger.LogWarning("Mesurement unmanaged {Mesurement}", JsonSerializer.Serialize(d));
                        continue;
                    }

                    var cas = capteurs.Where(c => c.IdMesure == d.Mesure);
                    Capteur? ca = cas.FirstOrDefault();

                    if (cas.Count() > 1)
                    {
                        _logger.LogWarning(
                            "More than one sensor is mathing the mesurement idMesure {IdMesure} from query event {EventStr}, {DevEUI}",
                            d.Mesure,
                            eventStr?.Sanatize(),
                            eventInfo.deviceInfo.devEui?.Sanatize());
                    }

                    if (ca != null)
                    {
                        ca.Online = true;
                        ca.LastMessageTime = DateTimeOffset.Now;

                        var newDonneesCapteur = new DonneeCapteur
                        {
                            IdCapteur = ca.Id,
                            Valeur = d.Value,
                            D = DateTimeOffset.Now,
                            Text = d.Text
                        };

                        await _context.DonneesCapteur.AddAsync(newDonneesCapteur, token);

                        await ExecuteAlerteFeatureAsync(ca, newDonneesCapteur, token);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "No sensor is matching the mesurment idMesure {IdMesure} from query event {EventStr}, {DevEUI}",
                            d.Mesure,
                            eventStr?.Sanatize(),
                            eventInfo.deviceInfo.devEui?.Sanatize());
                    }
                }
            }
            else
            {
                _logger.LogWarning("mesurementsResponse.Mesurements == null");
            }


            await _context.SaveChangesAsync(token);
        }
        finally
        {
            sem.Release();
        }

        return Ok();
    }

    private async Task ExecuteAlerteFeatureAsync(Capteur ca, DonneeCapteur newDonneesCapteur, CancellationToken token)
    {
        try
        {
            var alertes = await _context.AlerteCapteurs
                        .AsNoTracking()
                        .Where(a => a.IdCapteur == ca.Id && a.IsEnable)
                        .ToArrayAsync(token);

            for (int i = 0; i < alertes.Length; i++)
            {
                var alerte = alertes[i];

                await MaybeTriggerAlerte(
                    alerte,
                    newDonneesCapteur);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in ExecuteAlerteFeatureAsync of ChirpstackController");   
        }
    }

    private async Task MaybeTriggerAlerte(
        AlerteCapteur alerte,
        DonneeCapteur donnee)
    {
        var validationCount = 0;
        var conditionMet = 0;

        if (alerte.MinValue.HasValue)
        {
            validationCount++;

            if (donnee.Valeur <= alerte.MinValue.Value)
            {
                conditionMet++;
            }
        }

        if (alerte.MaxValue.HasValue)
        {
            validationCount++;

            if (donnee.Valeur >= alerte.MaxValue.Value)
            {
                conditionMet++;
            }
        }

        if (conditionMet > 0)
        {
            await TriggerAlerteCourriel(alerte, _logger, _emailConfig.Value, _emailService, donnee);
            await TriggerAlerteSMS(alerte, _logger, _smsConfig.Value, _smsService, donnee);
        }
        else
        {
            _logger.LogInformation("Alerte {AlerteId} {AlerteNom} not trigger", alerte.Id, alerte.Nom);
            _logger.LogInformation("Validation count greater that 0 {ValidationCountGt0} && validation count eqal conditionMet {ValidationCount} == {ConditionMet} = false", validationCount > 0, validationCount, conditionMet);
        }
    }

    private async Task ManageHistory(ChirpStackSrvConfig server, string? eventStr, PostChirpstackEvent eventInfo, CancellationToken token)
    {
        var history = await _context.ChirpstackMessageHistory
            .Where(h => h.ChirpStackSrvConfigId == server.Id)
            .OrderBy(h => h.Date)
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
            EventType = eventStr,
            Data = eventInfo.data,
            Date = DateTimeOffset.Now,
            DecodedData = JsonSerializer.Serialize(TryDecodeData(eventInfo.data, _logger), _ignoreDefaultVlue),
            MessageJson = JsonSerializer.Serialize(eventInfo)
        }, token);

        await _context.TrySaveChangesAsync(token);
    }

    private JsonSerializerOptions _ignoreDefaultVlue = new JsonSerializerOptions
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

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
    public async Task<IActionResult> EditConfigs(
        [FromRoute] Guid id, [FromBody] ChirpStackSrvConfig chirpStackSrvConfig, CancellationToken token)
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
