using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
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
        var config = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();

        if (!string.Equals(config["Abonnement.ValiderPlan"], "true", StringComparison.OrdinalIgnoreCase))
        {
            await base.OnActionExecutionAsync(context, next);
            return;
        }

        if (await PossedeAbonnementPermisAsync(context))
        {
            await base.OnActionExecutionAsync(context, next);
        }
        else
        {
            var forbidenReasonMessage = $"Un abonnement actif d'un des forfaits suivants est requis : {string.Join(", ", _plansPermis)}";
            context.HttpContext.Response.Headers["X-ErabliereApi-ForbidenReason"] = forbidenReasonMessage;
            context.Result = new ForbidResult();

            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<ValiderAbonnementAttribute>>();
            using var scope = context.HttpContext.RequestServices.CreateScope();
            logger.LogWarning("Access denied on {Path} : user {User} has no active subscription in plans [{Plans}]",
                context.HttpContext.Request.Path,
                UsersUtils.GetUniqueName(scope, context.HttpContext.User),
                string.Join(", ", _plansPermis));
        }
    }

    private async Task<bool> PossedeAbonnementPermisAsync(ActionExecutingContext context)
    {
        using var scope = context.HttpContext.RequestServices.CreateScope();

        var uniqueName = UsersUtils.GetUniqueName(scope, context.HttpContext.User);

        if (string.IsNullOrWhiteSpace(uniqueName))
        {
            return false;
        }

        var dbContext = context.HttpContext.RequestServices.GetRequiredService<ErabliereDbContext>();
        var token = context.HttpContext.RequestAborted;

        var customer = await dbContext.Customers.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UniqueName == uniqueName, token);

        if (customer == null)
        {
            return false;
        }

        var abonnements = await dbContext.Abonnements.AsNoTracking()
            .Where(a => a.CustomerId == customer.Id && a.Statut == StatutAbonnement.Actif)
            .ToArrayAsync(token);

        var maintenant = DateTimeOffset.Now;

        return Array.Exists(abonnements, a =>
            a.EstActif(maintenant) &&
            Array.Exists(_plansPermis, p => string.Equals(p, a.Plan, StringComparison.OrdinalIgnoreCase)));
    }
}
