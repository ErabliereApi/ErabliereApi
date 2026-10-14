using ErabliereApi.Donnees.Action.Post;
using Microsoft.AspNetCore.Mvc.Filters;
using static System.Text.Json.JsonSerializer;
using ErabliereApi.Services.Notifications;

namespace ErabliereApi.Attributes;

/// <summary>
/// Classe qui permet de rechercher et lancer les alertes relier à une action.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class TriggerAlertV4Attribute : ActionFilterAttribute
{
    /// <summary>
    /// Contructeur par initialisation.
    /// </summary>
    /// <param name="order">Ordre d'exectuion des action filter</param>
    public TriggerAlertV4Attribute(int order = int.MinValue)
    {
        Order = order;
    }

    /// <inheritdoc />
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        GetPostInfo(context);
    }

    private static void GetPostInfo(ActionExecutingContext context)
    {
        try
        {
            var _donnees = context.ActionArguments.Values.Single(a => a?.GetType() == typeof(PostDonneeCapteurV2[])) as PostDonneeCapteurV2[];

            context.HttpContext.Items.Add("TriggerAlertV4Attribute", _donnees);
        }
        catch (InvalidOperationException e)
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<TriggerAlertV4Attribute>>();

            logger.LogCritical(92837485, e, "typeof(PostDonneeCapteurV2[]) not found in {0}", Serialize(context.ActionArguments.Where(a => a.Value?.GetType() != typeof(CancellationToken))));

            throw new InvalidOperationException("Le paramètre PostDonneeCapteur[] est requis dans la route pour utiliser l'attribue 'TriggerAlertV4'.", e);
        }
    }

    /// <inheritdoc />
    public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        var result = await next();

        // Un lot refusé (400, 403, 404) n'a pas été enregistré et ne change l'état d'aucune alerte.
        // Un lot accepté ne contient que des capteurs de l'érablière de la route, le contrôleur l'a validé.
        if (!result.Canceled && context.HttpContext.Response.StatusCode is >= 200 and < 300)
        {
            try
            {
                var alerteCapteurService = context.HttpContext.RequestServices.GetRequiredService<AlerteCapteurService>();

                if (context.HttpContext.Items["TriggerAlertV4Attribute"] is not PostDonneeCapteurV2[] _donnees)
                {
                    throw new InvalidOperationException("Les informations de l'attribue 'TriggerAlertV4' n'ont pas été trouvé dans le contexte.");
                }

                // Pas le jeton de la requête : un client qui abandonne ne doit pas interrompre le changement d'état.
                await context.HttpContext.RequestServices.GetRequiredService<SilenceCapteurService>()
                    .SignalerReceptionAsync(_donnees.Where(d => d.IdCapteur.HasValue).Select(d => d.IdCapteur!.Value), CancellationToken.None);

                // En ordre chronologique : un lot envoyé en différé peut contenir une panne et son retour.
                foreach (var donnee in _donnees.Where(d => d.IdCapteur.HasValue).OrderBy(d => d.D))
                {
                    // Pas le jeton de la requête : un client qui abandonne ne doit pas interrompre le changement d'état.
                    await alerteCapteurService.EvaluerAsync(
                        donnee.IdCapteur!.Value,
                        donnee.V,
                        donnee.D ?? DateTimeOffset.Now,
                        CancellationToken.None);
                }
            }
            catch (Exception e)
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<TriggerAlertV4Attribute>>();

                logger.LogCritical(new EventId(92837485, "TriggerAlertV4Attribute.OnActionExecuted"), e, "Une erreur imprévue est survenu lors de l'execution de la fonction d'alertage.");
            }
        }
    }
}
