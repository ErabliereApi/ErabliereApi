using System.Net;
using System.Net.Sockets;
using ErabliereApi.Depot.Sql;
using ErabliereApi.Extensions;
using ErabliereApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ErabliereApi.Middlewares;

/// <summary>
/// Middleware pour capturer les informations IP des requêtes entrantes
/// </summary>
public class IpInfoMiddleware : IMiddleware
{
    private readonly ILogger<IpInfoMiddleware> _logger;
    private readonly ErabliereDbContext _context;
    private readonly IMemoryCache _memoryCache;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;

    /// <summary>
    /// Préfixe de la clé de cache pour les informations IP
    /// </summary>
    public const string CacheKeyPrefix = "IpInfo_";

    /// <summary>
    /// Constructeur
    /// </summary>
    public IpInfoMiddleware(
        ILogger<IpInfoMiddleware> logger,
        ErabliereDbContext dbContext,
        IMemoryCache memoryCache,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _context = dbContext;
        _memoryCache = memoryCache;
        _httpClientFactory = httpClientFactory;
        _config = configuration;
    }

    /// <inheritdoc />
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var ipAddress = context.GetClientIp();

        // Log the IP address
        _logger.LogInformation("Request from IP: {IpAddress}", ipAddress);

        try
        {
            var ipInfo = await IpResolutionAsync(context, ipAddress);

            if (ipInfo != null)
            {
                // Verify if the IP is allowed
                if (!ipInfo.IsAllowed)
                {
                    _logger.LogInformation("Blocked request from IP: {IpAddress}", ipAddress);
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync($"Access forbidden from your IP address: {ipAddress}");
                    return;
                }

                // Verify if the IP contry is in the authorized list
                var authorizedCountries = _config.GetSection("IpInfoApi:AuthorizeCountries").Get<List<string>>() ?? [];
                if (authorizedCountries.Any() &&
                    !string.IsNullOrEmpty(ipInfo.CountryCode) &&
                    !authorizedCountries.Contains(ipInfo.CountryCode, StringComparer.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Blocked request from IP: {IpAddress} due to unauthorized country: {CountryCode}", ipAddress, ipInfo.CountryCode);
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync($"Access forbidden from your country: {ipInfo.CountryCode}");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving IP information for {IpAddress}", ipAddress);
        }

        await next(context);
    }

    private async Task<IpInfo?> IpResolutionAsync(HttpContext context, string ipAddress)
    {
        // Check if the IP information is cached
        if (!_memoryCache.TryGetValue($"{CacheKeyPrefix}{ipAddress}", out IpInfo? ipInfo))
        {
            // If not cached, retrieve it from the database
            ipInfo = await _context.IpInfos.FirstOrDefaultAsync(i => i.Ip == ipAddress, context.RequestAborted);

            if (ipInfo != null && ipInfo.TTL >= DateTimeOffset.UtcNow)
            {
                _memoryCache.Set(ipAddress, ipInfo, _config.GetRequiredValue<TimeSpan>("IpInfoApi:CacheDuration"));
            }
            else if (ipInfo != null && (ipInfo.TTL == null || ipInfo.TTL < DateTimeOffset.UtcNow))
            {
                ipInfo = await UpdateFromIpInfoApi(context, ipAddress, ipInfo);
            }
            else
            {
                ipInfo = await ResolveFromIpInfoApi(context, ipAddress, ipInfo);
            }
        }

        // Attach the IP information to the context
        context.Items["IpInfo"] = ipInfo;

        return ipInfo;
    }

    private async Task<IpInfo> UpdateFromIpInfoApi(HttpContext context, string ipAddress, IpInfo ipInfo)
    {
        var client = _httpClientFactory.CreateClient("IpInfoClient");

        var ipInfoResponse = await client.GetAsync($"/lite/{ipAddress}", context.RequestAborted);

        if (ipInfoResponse.IsSuccessStatusCode)
        {
            var ipInfoContent = await ipInfoResponse.Content.ReadFromJsonAsync<IpInfoResponse>(cancellationToken: context.RequestAborted);

            if (ipInfoContent != null)
            {
                ipInfo.Country = ipInfoContent.Country;
                ipInfo.AS_Domain = ipInfoContent.As_domain;
                ipInfo.AS_Name = ipInfoContent.As_name;
                ipInfo.ASN = ipInfoContent.Asn;
                ipInfo.Continent = ipInfoContent.Continent;
                ipInfo.CountryCode = ipInfoContent.Country_code;
                ipInfo.DM = DateTimeOffset.UtcNow;
                ipInfo.TTL = DateTimeOffset.UtcNow.Add(_config.GetRequiredValue<TimeSpan>("IpInfoApi:DatabaseIpTTL"));
                ipInfo.IsAllowed = true;

                if (string.IsNullOrWhiteSpace(ipInfo.Network) && !string.IsNullOrWhiteSpace(ipInfo.ASN) && ipInfo.ASN != "NA")
                {
                    await AddASNInfoIfPossibleAsync(ipInfo, context.RequestAborted);
                }

                _context.IpInfos.Update(ipInfo);
                await _context.SaveChangesAsync(context.RequestAborted);

                // Cache the new IP information
                _memoryCache.Set($"{CacheKeyPrefix}{ipAddress}", ipInfo, _config.GetRequiredValue<TimeSpan>("IpInfoApi:CacheDuration"));
            }
        }

        return ipInfo;
    }

    private async Task<IpInfo?> ResolveFromIpInfoApi(HttpContext context, string ipAddress, IpInfo? ipInfo)
    {
        var client = _httpClientFactory.CreateClient("IpInfoClient");

        var ipInfoResponse = await client.GetAsync($"/lite/{ipAddress}", context.RequestAborted);

        if (ipInfoResponse.IsSuccessStatusCode)
        {
            var ipInfoContent = await ipInfoResponse.Content.ReadFromJsonAsync<IpInfoResponse>(cancellationToken: context.RequestAborted);

            if (ipInfoContent != null)
            {
                ipInfo = new IpInfo
                {
                    Ip = ipAddress,
                    Country = ipInfoContent.Country,
                    AS_Domain = ipInfoContent.As_domain,
                    AS_Name = ipInfoContent.As_name,
                    ASN = ipInfoContent.Asn,
                    Continent = ipInfoContent.Continent,
                    CountryCode = ipInfoContent.Country_code,
                    DC = DateTimeOffset.UtcNow,
                    TTL = DateTimeOffset.UtcNow.Add(_config.GetRequiredValue<TimeSpan>("IpInfoApi:DatabaseIpTTL")),
                    IsAllowed = true
                };

                if (!string.IsNullOrWhiteSpace(ipInfo.ASN) && ipInfo.ASN != "NA")
                {
                    await AddASNInfoIfPossibleAsync(ipInfo, context.RequestAborted);
                }
                await _context.IpInfos.AddAsync(ipInfo, context.RequestAborted);
                await _context.SaveChangesAsync(context.RequestAborted);

                // Cache the new IP information
                _memoryCache.Set($"{CacheKeyPrefix}{ipAddress}", ipInfo, _config.GetRequiredValue<TimeSpan>("IpInfoApi:CacheDuration"));
            }
        }

        return ipInfo;
    }

    private async Task AddASNInfoIfPossibleAsync(IpInfo ipInfo, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ipInfo.ASN) || ipInfo.ASN == "NA")
        {
            return;
        }

        try
        {
            var existingAsnInfo = await _context.IpNetworkAsnInfos
                .AsNoTracking()
                .Where(a => a.ASN == ipInfo.ASN && a.CountryCode == ipInfo.CountryCode)
                .ToArrayAsync(cancellationToken);

            if (existingAsnInfo != null)
            {
                var mostSpecificNetwork = FindMostSpecificNetwork(ipInfo.Ip, existingAsnInfo);

                if (mostSpecificNetwork != null)
                {
                    ipInfo.Network = mostSpecificNetwork.Network;
                    ipInfo.AS_Domain = mostSpecificNetwork.AS_Domain;
                    ipInfo.AS_Name = mostSpecificNetwork.AS_Name;
                }
                else
                {
                    _logger.LogWarning("No matching network found for IP: {Ip} with ASN: {Asn} and country: {Country}", ipInfo.Ip, ipInfo.ASN, ipInfo.CountryCode);
                }
            }
            else
            {
                _logger.LogWarning("ASN {Asn} information not found for IP: {Ip} and country: {Country}", ipInfo.ASN, ipInfo.Ip, ipInfo.CountryCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while trying to find ASN info for IP: {Ip} with ASN: {Asn} and country: {Country}", ipInfo.Ip, ipInfo.ASN, ipInfo.CountryCode);
        }
    }

    /// <summary>
    /// Trouve le réseau CIDR le plus spécifique contenant l'adresse IP donnée
    /// </summary>
    /// <param name="ipString">Adresse IP au format chaîne</param>
    /// <param name="cidrList">Liste des réseaux CIDR au format chaîne</param>
    private static IpNetworkAsnInfo? FindMostSpecificNetwork(string ipString, IEnumerable<IpNetworkAsnInfo> cidrList)
    {
        if (!IPAddress.TryParse(ipString, out var ip) || ip.AddressFamily != AddressFamily.InterNetwork)
            throw new ArgumentException("ipString doit être une adresse IPv4 valide.");

        uint ipUint = IPv4ToUInt(ip);
        IpNetworkAsnInfo? bestNetwork = null;
        int bestPrefix = -1;

        foreach (var cidr in cidrList)
        {
            var raw = cidr.Network;
            if (string.IsNullOrWhiteSpace(raw)) continue;
            var line = raw.Trim();
            if (line.StartsWith('-') || line.StartsWith('#')) continue;

            var parts = line.Split('/');
            if (parts.Length != 2) continue;

            if (!IPAddress.TryParse(parts[0], out var netIp) || netIp.AddressFamily != AddressFamily.InterNetwork) continue;
            if (!int.TryParse(parts[1], out int prefix) || prefix < 0 || prefix > 32) continue;

            uint netUint = IPv4ToUInt(netIp);
            uint mask = prefix == 0 ? 0u : (0xFFFFFFFFu << (32 - prefix));
            if ((ipUint & mask) == (netUint & mask) && prefix > bestPrefix)
            {
                bestPrefix = prefix;
                bestNetwork = cidr;
            }
        }

        return bestNetwork;
    }

    // Convertit une IP IPv4 en uint (octet0 << 24 | octet1 << 16 | ...)
    static uint IPv4ToUInt(IPAddress ip)
    {
        var b = ip.GetAddressBytes();
        return ((uint)b[0] << 24) | ((uint)b[1] << 16) | ((uint)b[2] << 8) | b[3];
    }
}