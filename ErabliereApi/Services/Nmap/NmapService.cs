using System.Xml;
using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using Microsoft.EntityFrameworkCore;

namespace ErabliereApi.Services.Nmap;

/// <summary>
/// Service for handling Nmap scans
/// </summary>
public class NmapService
{
    private readonly ErabliereDbContext _context;
    private readonly ILogger<NmapService> _logger;

    /// <summary>
    /// State attribute name in XML
    /// </summary>
    public const string STATE = "state";

    /// <summary>
    /// Reason attribute name in XML
    /// </summary>
    public const string REASON = "reason";

    /// <summary>
    /// Constructeur
    /// </summary>
    /// <param name="context"></param>
    /// <param name="logger"></param>
    public NmapService(ErabliereDbContext context, ILogger<NmapService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Met à jour les appareils à partir d'un scan Nmap
    /// </summary>
    /// <param name="idErabliere"></param>
    /// <param name="nmapResult"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public async Task UpdateDevicesFromNmapScanAsync(Guid idErabliere, XmlDocument nmapResult, CancellationToken token)
    {
        var existingDevices = await _context.Appareils
            .Include(a => a.Statut)
            .Include(a => a.Adresses)
            .Include(a => a.Ports)
            .Include(a => a.NomsHost)
            .Where(a => a.IdErabliere == idErabliere)
            .ToListAsync(token);

        // On traite les hosthint
        await ParseHostsHintAsync(idErabliere, nmapResult, existingDevices, token);

        // On traite les hosts
        await ParseHostsAsync(nmapResult, existingDevices, token);
    }

    private async Task ParseHostsHintAsync(Guid idErabliere, XmlDocument nmapResult, List<Appareil> existingDevices, CancellationToken token)
    {
        var hosthint = nmapResult.SelectNodes("/nmaprun/hosthint");

        if (hosthint != null && hosthint.Count > 0)
        {
            foreach (XmlNode host in hosthint)
            {
                await ParseHostHintAsync(idErabliere, existingDevices, host, token);
            }

            await _context.SaveChangesAsync(token);
        }
    }

    private async Task ParseHostHintAsync(Guid idErabliere, List<Appareil> existingDevices, XmlNode host, CancellationToken token)
    {
        var address = host.SelectSingleNode("address")?.Attributes?["addr"]?.Value;
        if (string.IsNullOrWhiteSpace(address))
        {
            _logger.LogWarning("No address found for hosthint");
            return;
        }
        var appareil = existingDevices.FirstOrDefault(a => a.Adresses.Any(ad => ad.Addr == address));
        if (appareil == null)
        {
            await AddNewAppareilFromHostHint(idErabliere, existingDevices, host, address, token);
        }
        else
        {
            var statut = host.SelectSingleNode("status");
            if (statut != null && statut.Attributes != null)
            {
                if (appareil.Statut == null)
                {
                    appareil.Statut = new AppareilStatut();
                }

                appareil.Statut.Etat = statut.Attributes[STATE]?.Value ?? "";
                appareil.Statut.Raison = statut.Attributes[REASON]?.Value ?? "";
                appareil.Statut.RaisonTTL = statut.Attributes["raison_ttl"]?.Value ?? "";
            }
        }
    }

    private async Task<Appareil> AddNewAppareilFromHostHint(Guid idErabliere, List<Appareil> existingDevices, XmlNode host, string address, CancellationToken token)
    {
        Appareil appareil = new Appareil
        {
            IdErabliere = idErabliere,
            Name = address,
            Description = "Ajouté par scan nmap",
        };
        var addresses = host.SelectNodes("address");
        if (addresses != null)
        {
            foreach (XmlNode addr in addresses)
            {
                appareil.Adresses.Add(new AppareilAdresse
                {
                    Addr = addr.Attributes?["addr"]?.Value ?? "",
                    Addrtype = addr.Attributes?["addrtype"]?.Value ?? "",
                    Vendeur = addr.Attributes?["vendor"]?.Value ?? ""
                });
            }
        }
        var statut = host.SelectSingleNode("status");
        if (statut != null && statut.Attributes != null)
        {
            appareil.Statut = new AppareilStatut
            {
                Etat = statut.Attributes[STATE]?.Value ?? "",
                Raison = statut.Attributes[REASON]?.Value ?? "",
                RaisonTTL = statut.Attributes["reason_ttl"]?.Value ?? ""
            };
        }
        await _context.Appareils.AddAsync(appareil, token);
        existingDevices.Add(appareil);
        return appareil;
    }

    private async Task ParseHostsAsync(XmlDocument nmapResult, List<Appareil> existingDevices, CancellationToken token)
    {
        var hosts = nmapResult.SelectNodes("/nmaprun/host");

        if (hosts != null)
        {
            foreach (XmlNode host in hosts)
            {
                ParseHost(existingDevices, host);
            }

            await _context.SaveChangesAsync(token);
        }
    }

    private void ParseHost(List<Appareil> existingDevices, XmlNode host)
    {
        var address = host.SelectSingleNode("address");
        if (address == null)
        {
            _logger.LogWarning("No address found for host");
            return;
        }
        if (address.Attributes?["addr"] == null)
        {
            _logger.LogWarning("No addr attribute found for address");
            return;
        }
        var addr = address.Attributes["addr"]?.Value;
        var appareil = existingDevices.FirstOrDefault(a => a.Adresses.Any(ad => ad.Addr == addr));
        if (appareil == null)
        {
            _logger.LogWarning("No device found for address {Address}, skipping host", addr);
            return;
        }
        MapTimesInfo(host, appareil);
        var ports = host.SelectNodes("ports/port");
        if (ports != null)
        {
            foreach (XmlNode port in ports)
            {
                MapPort(appareil, port);
            }
        }
        var hostNames = host.SelectNodes("hostnames/hostname");
        if (hostNames != null)
        {
            foreach (XmlNode hostname in hostNames)
            {
                MapHostName(appareil, hostname);
            }
        }
    }

    private static void MapTimesInfo(XmlNode host, Appareil appareil)
    {
        appareil.Temps.DateDebut = host.Attributes?["starttime"] != null
            ? long.Parse(host.Attributes["starttime"]?.Value ?? "0")
            : 0;
        appareil.Temps.DateFin = host.Attributes?["endtime"] != null
            ? long.Parse(host.Attributes["endtime"]?.Value ?? "0")
            : 0;
        var timeXml = host.SelectSingleNode("times");
        if (timeXml != null && timeXml.Attributes != null)
        {
            appareil.Temps.Strtt = timeXml.Attributes["srtt"] != null
                ? long.Parse(timeXml.Attributes["srtt"]?.Value ?? "0")
                : 0;
            appareil.Temps.Rttvar = timeXml.Attributes["rttvar"] != null
                ? long.Parse(timeXml.Attributes["rttvar"]?.Value ?? "0")
                : 0;
            appareil.Temps.To = timeXml.Attributes["to"] != null
                ? long.Parse(timeXml.Attributes["to"]?.Value ?? "0")
                : 0;
        }
    }

    private void MapHostName(Appareil appareil, XmlNode hostname)
    {
        var name = hostname?.Attributes?["name"];
        var type = hostname?.Attributes?["type"];

        var existing = appareil.NomsHost.FirstOrDefault(n => n.Name == name?.Value);
        if (existing != null)
        {
            existing.Type = type?.Value ?? "";
        }
        else
        {
            appareil.NomsHost.Add(new NomHostAppareil
            {
                Name = name?.Value ?? "",
                Type = type?.Value ?? ""
            });
        }
    }

    private void MapPort(Appareil appareil, XmlNode port)
    {
        var portIdStr = port.Attributes?["portid"]?.Value;
        var state = port.SelectSingleNode(STATE);
        var service = port.SelectSingleNode("service");
        if (!int.TryParse(portIdStr, out int portId))
        {
            _logger.LogWarning("No portid found for port");
            return;
        }
        if (appareil.Ports.Any(p => p.Port == portId))
        {
            foreach (var existingPort in appareil.Ports.Where(p => p.Port == portId))
            {
                existingPort.Protocole = port.Attributes?["protocol"]?.Value ?? "";
                existingPort.Etat = new PortEtat
                {
                    Etat = state?.Attributes?[STATE]?.Value ?? "",
                    Raison = state?.Attributes?[REASON]?.Value ?? "",
                    RaisonTTL = state?.Attributes?["reason_ttl"]?.Value ?? "",
                };
                existingPort.PortService = new PortService
                {
                    Name = service?.Attributes?["name"]?.Value ?? "",
                    Produit = service?.Attributes?["product"]?.Value ?? "",
                    ExtraInfo = service?.Attributes?["extrainfo"]?.Value ?? "",
                    Methode = service?.Attributes?["method"]?.Value ?? ""
                };
            }
        }
        else
        {
            appareil.Ports.Add(new PortAppareil
            {
                Port = portId,
                Protocole = port.Attributes?["protocol"]?.Value ?? "",
                Etat = new PortEtat
                {
                    Etat = state?.Attributes?[STATE]?.Value ?? "",
                    Raison = state?.Attributes?[REASON]?.Value ?? "",
                    RaisonTTL = state?.Attributes?["reason_ttl"]?.Value ?? "",
                },
                PortService = new PortService
                {
                    Name = service?.Attributes?["name"]?.Value ?? "",
                    Methode = service?.Attributes?["method"]?.Value ?? "",
                    Produit = service?.Attributes?["product"]?.Value ?? "",
                    ExtraInfo = service?.Attributes?["extrainfo"]?.Value ?? ""
                }
            });
        }
    }
}