using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Extensions;
using ErabliereApi.Services.Abonnements;
using ErabliereApi.Services.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace ErabliereApi.Attributes;

/// <summary>
/// Restreint l'accès à une fonctionnalité selon le forfait d'abonnement de l'utilisateur.
/// L'utilisateur doit posséder un abonnement actif dont le forfait fait partie des forfaits permis.
///
/// La validation n'est effectuée que si la clé de configuration 'Abonnement.ValiderPlan'
/// vaut "true". Sinon, l'attribut est sans effet, ce qui permet de déployer le gating
/// progressivement sans changer le comportement existant.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ValiderAbonnementAttribute : ActionFilterAttribute
{
    private readonly string[] _plansPermis;

    /// <summary>
    /// Valider que l'utilisateur possède un abonnement actif d'un des forfaits permis
    /// </summary>
    /// <param name="plansPermis">Les forfaits donnant accès à la fonctionnalité.
    /// Voir <see cref="ForfaitsAbonnement" /> pour les valeurs permises.</param>
    public ValiderAbonnementAttribute(params string[] plansPermis)
    {
        if (plansPermis.Length == 0)
        {
            throw new ArgumentException("Au moins un forfait permis doit être spécifié.", nameof(plansPermis));
        }

        _plansPermis = plansPermis;
    }

    /// <summary>
    /// Vérifie l'abonnement de l'utilisateur avant d'exécuter l'action
    /// </summary>
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (await EstPermisAsync(context.HttpContext, _plansPermis))
        {
            await base.OnActionExecutionAsync(context, next);
        }
        else
        {
            context.Result = new ForbidResult();
        }
    }

    /// <summary>
    /// Vérifie que l'utilisateur de la requête possède un abonnement actif d'un des
    /// forfaits permis. Toujours vrai lorsque 'Abonnement.ValiderPlan' ne vaut pas "true".
    /// </summary>
    /// <remarks>
    /// Partagé avec les endpoints qui ne passent pas par MVC (l'endpoint AG-UI de
    /// CopilotKit) afin que la règle reste à un seul endroit. En cas de refus, l'en-tête
    /// 'X-ErabliereApi-ForbidenReason' est ajouté à la réponse et le refus est journalisé ;
    /// il revient à l'appelant de produire la réponse 403.
    /// </remarks>
    public static async Task<bool> EstPermisAsync(HttpContext httpContext, string[] plansPermis)
    {
        var config = httpContext.RequestServices.GetRequiredService<IConfiguration>();

        if (!string.Equals(config["Abonnement.ValiderPlan"], "true", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (await PossedeAbonnementPermisAsync(httpContext, plansPermis))
        {
            return true;
        }

        var forbidenReasonMessage = $"Un abonnement actif d'un des forfaits suivants est requis : {string.Join(", ", plansPermis)}";
        httpContext.Response.Headers["X-ErabliereApi-ForbidenReason"] = forbidenReasonMessage;

        var logger = httpContext.RequestServices.GetRequiredService<ILogger<ValiderAbonnementAttribute>>();
        using var scope = httpContext.RequestServices.CreateScope();
        logger.LogWarning("Access denied on {Path} : user {User} has no active subscription in plans [{Plans}]",
            httpContext.Request.Path.ToString().Sanatize(),
            UsersUtils.GetUniqueName(scope, httpContext.User),
            string.Join(", ", plansPermis));

        return false;
    }

    private static async Task<bool> PossedeAbonnementPermisAsync(HttpContext httpContext, string[] plansPermis)
    {
        using var scope = httpContext.RequestServices.CreateScope();

        var uniqueName = UsersUtils.GetUniqueName(scope, httpContext.User);

        if (string.IsNullOrWhiteSpace(uniqueName))
        {
            return false;
        }

        var dbContext = httpContext.RequestServices.GetRequiredService<ErabliereDbContext>();
        var token = httpContext.RequestAborted;

        var customer = await dbContext.Customers.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UniqueName == uniqueName, token);

        if (customer?.Id == null)
        {
            return false;
        }

        // Le forfait courant vient du service d'abonnement plutôt que d'une requête
        // écrite ici : la règle « quel forfait cet utilisateur a-t-il en ce moment »
        // doit rester à un seul endroit, aussi consommé par GET /api/Abonnements/Courant
        // et par le serveur MCP.
        var abonnementService = httpContext.RequestServices.GetRequiredService<IAbonnementService>();

        var plan = await abonnementService.ObtenirPlanCourantAsync(customer.Id.Value, DateTimeOffset.Now, token);

        return Array.Exists(plansPermis, p => string.Equals(p, plan, StringComparison.OrdinalIgnoreCase));
    }
}
