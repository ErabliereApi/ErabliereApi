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
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        // Log the IP address
        _logger.LogInformation("Request from IP: {IpAddress}", ipAddress);

        try
        {
            await IpLookupInSaves(context, ipAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving IP information for {IpAddress}", ipAddress);
        }

        await next(context);
    }

    private async Task IpLookupInSaves(HttpContext context, string ipAddress)
    {
        // Check if the IP information is cached
        if (!_memoryCache.TryGetValue(ipAddress, out IpInfo? ipInfo))
        {
            // If not cached, retrieve it from the database
            ipInfo = await _context.IpInfos.FirstOrDefaultAsync(i => i.Ip == ipAddress, context.RequestAborted);

            if (ipInfo != null)
            {
                // Cache the IP information
                _memoryCache.Set(ipAddress, ipInfo, TimeSpan.FromMinutes(5));
            }
            else
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
                            IsAllowed = true
                        };

                        await _context.IpInfos.AddAsync(ipInfo, context.RequestAborted);
                        await _context.SaveChangesAsync(context.RequestAborted);

                        // Cache the new IP information
                        _memoryCache.Set(ipAddress, ipInfo, _config.GetRequiredValue<TimeSpan>("IpInfoApi:CacheDuration"));
                    }
                }
            }
        }

        // Attach the IP information to the context
        context.Items["IpInfo"] = ipInfo;
    }
}