using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Extensions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;

namespace ErabliereApi.Attributes;

/// <summary>
/// Attribue permettant de restraindre les accès a un seul adresse IP selon l'id enregistré dans les données de l'érablière.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class ValiderIPRulesAttribute : ActionFilterAttribute
{
    /// <summary>
    /// Contructeur par initialisation.
    /// </summary>
    /// <param name="order">Ordre d'exectuion des action filter</param>
    public ValiderIPRulesAttribute(int order = int.MinValue)
    {
        Order = order;
    }

    /// <inheritdoc />
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var id = context.ActionArguments["id"];

        var cache = context.HttpContext.RequestServices.GetRequiredService<IDistributedCache>();

        var erabliere = await cache.GetAsync<Erabliere>($"Erabliere_{id}", context.HttpContext.RequestAborted);

        if (erabliere == null) 
        {
            var depot = context.HttpContext.RequestServices.GetRequiredService<ErabliereDbContext>();

            erabliere = await depot.Erabliere.FindAsync([id], context.HttpContext.RequestAborted);

            if (erabliere != null) 
            {
                await cache.SetAsync($"Erabliere_{id}", erabliere, context.HttpContext.RequestAborted);
            }
        }
        

        if (!string.IsNullOrWhiteSpace(erabliere?.IpRule) && erabliere.IpRule != "-")
        {
            var ip = context.HttpContext.GetClientIp();

            if (!context.ModelState.ContainsKey(HttpContextExtension.RealIPKey) &&
                !Array.TrueForAll(erabliere.IpRule.Split(';'), eIp => string.Equals(eIp, ip, StringComparison.OrdinalIgnoreCase)))
            {
                context.ModelState.AddModelError("IP", $"L'adresse IP est différente de l'adresse ip aloué pour créer des alimentations à cette érablière. L'adresse IP reçu est {ip}.");
            }
        }

        await next();
    }
}
