using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Donnees.Action.Post;
using Microsoft.AspNetCore.Mvc.Filters;
using static System.Text.Json.JsonSerializer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ErabliereApi.Services;
using static ErabliereApi.Services.AlerteHelper;

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
        GetIdAndPostInfo(context);
    }

    private static void GetIdAndPostInfo(ActionExecutingContext context)
    {
        string id = context.ActionArguments["id"]?.ToString() ??
            throw new InvalidOperationException("Le paramètre Id est requis dans la route pour utiliser l'attribue 'TriggerAlertV4'.");

        var _idErabliere = Guid.Parse(id);

        try
        {
            var _donnee = context.ActionArguments.Values.Single(a => a?.GetType() == typeof(PostDonneeCapteurV2[])) as PostDonneeCapteurV2;

            context.HttpContext.Items.Add("TriggerAlertV4Attribute", (_idErabliere, _donnee));
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

        if (!result.Canceled)
        {
            try
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<TriggerAlertV4Attribute>>();
                var depot = context.HttpContext.RequestServices.GetRequiredService<ErabliereDbContext>();
                var emailConfig = context.HttpContext.RequestServices.GetRequiredService<IOptions<EmailConfig>>();
                var emailService = context.HttpContext.RequestServices.GetRequiredService<IEmailService>();
                var smsConfig = context.HttpContext.RequestServices.GetRequiredService<IOptions<SMSConfig>>();
                var smsService = context.HttpContext.RequestServices.GetRequiredService<ISmsService>();

                var contextTupple = context.HttpContext.Items["TriggerAlertV4Attribute"];

                if (contextTupple == null)
                {
                    throw new InvalidOperationException("Les informations de l'attribue 'TriggerAlertV3' n'ont pas été trouvé dans le contexte.");
                }

                var (_idErabliere, _donnees) = ((Guid, PostDonneeCapteurV2[]?))contextTupple;

                if (_donnees == null)
                {
                    throw new InvalidOperationException("Le paramètre '_donnees' doit être initialisé pour utiliser la fonction d'alertage.");
                }

                foreach (var donnee in _donnees)
                {
                    var alertes = await depot.AlerteCapteurs.AsNoTracking()
                        .Where(a => a.OwnerId == _idErabliere &&
                                    a.IdCapteur == donnee.IdCapteur && 
                                    a.IsEnable)
                        .ToArrayAsync();

                    for (int i = 0; i < alertes.Length; i++)
                    {
                        var alerte = alertes[i];

                        await MaybeTriggerAlerte(
                            alerte,
                            logger,
                            emailConfig.Value,
                            emailService,
                            smsConfig.Value,
                            smsService,
                            donnee);
                    }
                }
            }
            catch (Exception e)
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<TriggerAlertV3Attribute>>();

                logger.LogCritical(new EventId(92837485, "TriggerAlertV3Attribute.OnActionExecuted"), e, "Une erreur imprévue est survenu lors de l'execution de la fonction d'alertage.");
            }
        }
    }

    private static async Task MaybeTriggerAlerte(
        AlerteCapteur alerte,
        ILogger<TriggerAlertV4Attribute> logger,
        EmailConfig emailConfig,
        IEmailService emailService,
        SMSConfig smsConfig,
        ISmsService smsService,
        PostDonneeCapteurV2? _donnee)
    {
        if (_donnee == null)
        {
            throw new InvalidOperationException("Le paramètre '_donnee' doit être initialisé pour utiliser la fonction d'alertage.");
        }

        var validationCount = 0;
        var conditionMet = 0;

        if (alerte.MinValue.HasValue)
        {
            validationCount++;

            if (_donnee.V <= alerte.MinValue.Value)
            {
                conditionMet++;
            }
        }

        if (alerte.MaxValue.HasValue)
        {
            validationCount++;

            if (_donnee.V >= alerte.MaxValue.Value)
            {
                conditionMet++;
            }
        }

        if (conditionMet > 0)
        {
            await TriggerAlerteCourriel(alerte, logger, emailConfig, emailService, _donnee);
            await TriggerAlerteSMS(alerte, logger, smsConfig, smsService, _donnee);
        }
        else
        {
            logger.LogInformation("Alerte {AlerteId} {AlerteNom} not trigger", alerte.Id, alerte.Nom);
            logger.LogInformation("Validation count greater that 0 {ValidationCountGt0} && validation count eqal conditionMet {ValidationCount} == {ConditionMet} = false", validationCount > 0, validationCount, conditionMet);
        }
    }
}
