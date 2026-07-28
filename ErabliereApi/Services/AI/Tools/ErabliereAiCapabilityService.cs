using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Mcp.Configuration;
using ErabliereApi.Services.Abonnements;
using ErabliereApi.Services.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ErabliereApi.Services.AI.Tools;

/// <summary>
/// Default <see cref="IErabliereAiCapabilityService" />.
/// </summary>
/// <remarks>
/// The plan to capability mapping is the one of the MCP server
/// (<c>Mcp:PlanGating</c>, phase 4): a deployment that decided the 'mcp' capability
/// is what opens its data to an AI says it once, and both the MCP endpoint and the
/// chat obey it. The plan itself comes from <see cref="IAbonnementService" />, the
/// single authority the API and <c>ValiderAbonnementAttribute</c> already use — no
/// http call and no second definition of "who is on what plan".
/// </remarks>
public class ErabliereAiCapabilityService : IErabliereAiCapabilityService
{
    private readonly ErabliereDbContext _depot;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOptions<ErabliereAiToolOptions> _toolOptions;
    private readonly IOptions<McpPlanGatingOptions> _gatingOptions;

    /// <summary>
    /// Constructeur par initialisation
    /// </summary>
    /// <param name="serviceProvider">
    /// Sert à résoudre <see cref="IAbonnementService" />, qui n'est enregistré que
    /// lorsque l'intégration Stripe est activée. Une dépendance déclarée au
    /// constructeur ferait échouer le démarrage d'un déploiement sans abonnements,
    /// alors que ce cas a une réponse simple : personne n'est abonné.
    /// </param>
    public ErabliereAiCapabilityService(
        ErabliereDbContext depot,
        IServiceProvider serviceProvider,
        IHttpContextAccessor httpContextAccessor,
        IOptions<ErabliereAiToolOptions> toolOptions,
        IOptions<McpPlanGatingOptions> gatingOptions)
    {
        _depot = depot;
        _serviceProvider = serviceProvider;
        _httpContextAccessor = httpContextAccessor;
        _toolOptions = toolOptions;
        _gatingOptions = gatingOptions;
    }

    /// <inheritdoc />
    public async Task<ErabliereAiCapabilities> GetCapabilitiesAsync(CancellationToken token)
    {
        var gating = _gatingOptions.Value;
        var plansGrantingAccess = gating.PlansGrantingAccess();

        if (!_toolOptions.Value.Enabled)
        {
            // The operator turned the feature off entirely; that is not something a
            // subscription could fix, so no upgrade hint is offered.
            return new ErabliereAiCapabilities(false, ForfaitsAbonnement.Gratuit, false, [], null);
        }

        var plan = await ResolvePlanAsync(gating, token);

        if (!gating.Enabled)
        {
            return new ErabliereAiCapabilities(true, plan, false, plansGrantingAccess, gating.SubscriptionUrl);
        }

        return new ErabliereAiCapabilities(
            gating.GrantsAccess(plan),
            plan,
            PlanGateEnabled: true,
            plansGrantingAccess,
            gating.SubscriptionUrl);
    }

    private async Task<string> ResolvePlanAsync(McpPlanGatingOptions gating, CancellationToken token)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        var abonnementService = _serviceProvider.GetService<IAbonnementService>();

        if (httpContext is null || abonnementService is null)
        {
            return gating.DefaultPlan;
        }

        // RequestServices, not a child scope: ApiKeyAuthorizationContext is scoped and
        // filled by ApiKeyMiddleware in the request scope, so a child scope would hand
        // back an empty one and no api key caller would ever be recognized.
        var uniqueName = UsersUtils.GetUniqueName(httpContext.RequestServices, httpContext.User);

        if (string.IsNullOrWhiteSpace(uniqueName))
        {
            return gating.DefaultPlan;
        }

        var customer = await _depot.Customers.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UniqueName == uniqueName, token);

        if (customer?.Id is null)
        {
            return gating.DefaultPlan;
        }

        return await abonnementService.ObtenirPlanCourantAsync(customer.Id.Value, DateTimeOffset.Now, token);
    }
}
