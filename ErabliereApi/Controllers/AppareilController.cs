using ErabliereApi.Attributes;
using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
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
    private readonly ILogger<AppareilController> _logger;
    private readonly ErabliereDbContext _context;

    /// <summary>
    /// Constructeur
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="context"></param>
    public AppareilController(ILogger<AppareilController> logger, ErabliereDbContext context)
    {
        _logger = logger;
        _context = context;
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

        var existingDevices = await _context.Appareils.Where(a => a.IdErabliere == id).ToArrayAsync(token);

        // On traite les hosthint

        var hosthint = nmapObj.SelectNodes("/nmaprun/hosthint");

        if (hosthint != null)
        {
            foreach (System.Xml.XmlNode host in hosthint)
            {
                var address = host.SelectSingleNode("address")?.Attributes?["addr"]?.Value;
                if (string.IsNullOrWhiteSpace(address))
                {
                    _logger.LogWarning("No address found for hosthint");
                    continue;
                }
                var appareil = existingDevices.FirstOrDefault(a => a.Adresses.Any(ad => ad.Addr == address));
                if (appareil == null)
                {
                    appareil = new Appareil
                    {
                        IdErabliere = id,
                        Name = address,
                        Description = "Ajouté par scan nmap",
                        Adresses = new List<AppareilAdresse>
                        {
                            new AppareilAdresse
                            {
                                Addr = address
                            }
                        }
                    };
                    await _context.Appareils.AddAsync(appareil, token);
                    existingDevices = existingDevices.Append(appareil).ToArray();
                }
            }
        }

        await _context.SaveChangesAsync(token);

        // On traite les host

        var hosts = nmapObj.SelectNodes("/nmaprun/host");

        if (hosts != null)
        {
            foreach (System.Xml.XmlNode host in hosts)
            {
                var address = host.SelectSingleNode("address")?.Attributes?["addr"]?.Value;
                if (string.IsNullOrWhiteSpace(address))
                {
                    _logger.LogWarning("No address found for host");
                    continue;
                }
                var appareil = existingDevices.FirstOrDefault(a => a.Adresses.Any(ad => ad.Addr == address));
                if (appareil == null)
                {
                    _logger.LogWarning("No device found for address {address}, skipping host", address);
                    continue;
                }
                var ports = host.SelectNodes("ports/port");
                if (ports != null)
                {
                    foreach (System.Xml.XmlNode port in ports)
                    {
                        var portIdStr = port.Attributes?["portid"]?.Value;
                        if (!int.TryParse(portIdStr, out int portId))
                        {
                            _logger.LogWarning("No portid found for port");
                            continue;
                        }
                        var protocol = port.Attributes?["protocol"]?.Value ?? "tcp";
                        if (appareil.Ports.Any(p => p.Port == portId))
                        {
                            
                        }
                        else
                        {
                            appareil.Ports.Add(new PortAppareil
                            {
                                Port = portId,
                                PortService = new PortService
                                {
                                    
                                }
                            });
                        }
                    }
                }
            }

            await _context.SaveChangesAsync(token);
        }

        return NoContent();
    }
}
