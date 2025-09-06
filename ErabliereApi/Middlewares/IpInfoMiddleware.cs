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

                await _context.IpInfos.AddAsync(ipInfo, context.RequestAborted);
                await _context.SaveChangesAsync(context.RequestAborted);

                // Cache the new IP information
                _memoryCache.Set($"{CacheKeyPrefix}{ipAddress}", ipInfo, _config.GetRequiredValue<TimeSpan>("IpInfoApi:CacheDuration"));
            }
        }

        return ipInfo;
    }
}