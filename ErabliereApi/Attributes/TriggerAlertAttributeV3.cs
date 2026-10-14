using ErabliereApi.Donnees.Action.Post;
using Microsoft.AspNetCore.Mvc.Filters;
using static System.Text.Json.JsonSerializer;
using ErabliereApi.Services.Notifications;

namespace ErabliereApi.Attributes;

/// <summary>
/// Classe qui permet de rechercher et lancer les alertes relier à une action.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class TriggerAlertV3Attribute : ActionFilterAttribute
{
    /// <summary>
    /// Contructeur par initialisation.
    /// </summary>
    /// <param name="order">Ordre d'exectuion des action filter</param>
    public TriggerAlertV3Attribute(int order = int.MinValue)
    {
        Order = order;
    }

    /// <inheritdoc />
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        GetIdAndPostInfo(context);
    }

    private static void GetIdAndPostInfo(ActionExecutingContext context)
    {
        string id = context.ActionArguments["id"]?.ToString() ??
            throw new InvalidOperationException("Le paramètre Id est requis dans la route pour utiliser l'attribue 'TriggerAlertV3'.");

        var _idCapteur = Guid.Parse(id);

        try
        {
            var _donnee = context.ActionArguments.Values.Single(a => a?.GetType() == typeof(PostDonneeCapteurV2)) as PostDonneeCapteurV2;

            context.HttpContext.Items.Add("TriggerAlertV3Attribute", (_idCapteur, _donnee));
        }
        catch (InvalidOperationException e)
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<TriggerAlertV3Attribute>>();

            logger.LogCritical(92837485, e, "typeof(PostDonneeCapteurV2) not found in {0}", Serialize(context.ActionArguments.Where(a => a.Value?.GetType() != typeof(CancellationToken))));

            throw new InvalidOperationException("Le paramètre PostDonneeCapteur est requis dans la route pour utiliser l'attribue 'TriggerAlertV3'.", e);
        }
    }

    /// <inheritdoc />
    public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        var result = await next();

        // Une donnée refusée (400, 403, 404) n'a pas été enregistrée et ne change l'état d'aucune alerte.
        if (!result.Canceled && context.HttpContext.Response.StatusCode is >= 200 and < 300)
        {
            try
            {
                var alerteCapteurService = context.HttpContext.RequestServices.GetRequiredService<AlerteCapteurService>();

                var contextTupple = context.HttpContext.Items["TriggerAlertV3Attribute"];

                if (contextTupple == null)
                {
                    throw new InvalidOperationException("Les informations de l'attribue 'TriggerAlertV3' n'ont pas été trouvé dans le contexte.");
                }

                var (_idCapteur, _donnee) = ((Guid, PostDonneeCapteurV2?))contextTupple;

                if (_donnee == null)
                {
                    throw new InvalidOperationException("Le paramètre '_donnee' doit être initialisé pour utiliser la fonction d'alertage.");
                }

                // Pas le jeton de la requête : un client qui abandonne ne doit pas interrompre le changement d'état.
                await context.HttpContext.RequestServices.GetRequiredService<SilenceCapteurService>()
                    .SignalerReceptionAsync([_idCapteur], CancellationToken.None);

                await alerteCapteurService.EvaluerAsync(
                    _idCapteur,
                    _donnee.V,
                    _donnee.D ?? DateTimeOffset.Now,
                    CancellationToken.None);
            }
            catch (Exception e)
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<TriggerAlertV3Attribute>>();

                logger.LogCritical(new EventId(92837485, "TriggerAlertV3Attribute.OnActionExecuted"), e, "Une erreur imprévue est survenu lors de l'execution de la fonction d'alertage.");
            }
        }
    }
}
