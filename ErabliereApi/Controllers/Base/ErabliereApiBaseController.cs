using ErabliereApi.Authorization;
using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Extensions;
using ErabliereApi.Services.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErabliereApi.Controllers.Base;

/// <summary>
/// Contrôler de base pour le projet
/// </summary>
public abstract class ErabliereApiBaseController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ErabliereDbContext _context;
    private readonly IConfiguration _config;

    /// <summary>
    /// Constructeur
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="context"></param>
    /// <param name="config"></param>
    public ErabliereApiBaseController(
        IServiceProvider serviceProvider,
        ErabliereDbContext context,
        IConfiguration config
        )
    {
        _serviceProvider = serviceProvider;
        _context = context;
        _config = config;
    }

    /// <summary>
    /// Retourne un tuple (isAuthenticate, authenticationMethod (ApiKey ou Bearer), Customer)
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    protected async Task<(bool, string, Customer?)> IsAuthenticatedAsync(CancellationToken token)
    {
        if (User?.Identity?.IsAuthenticated == true)
        {
            using var scope = _serviceProvider.CreateScope();

            var unique_name = UsersUtils.GetUniqueName(scope, User);

            var customer = await _context.Customers.SingleAsync(c => c.UniqueName == unique_name, token);

            return (true, "Bearer", customer);
        }

        if (_config.StripeIsEnabled())
        {
            var apiKeyAuthContext = HttpContext?.RequestServices.GetRequiredService<ApiKeyAuthorizationContext>();

            return (apiKeyAuthContext?.Authorize == true, "ApiKey", apiKeyAuthContext?.Customer);
        }

        return (false, "", null);
    }
}
