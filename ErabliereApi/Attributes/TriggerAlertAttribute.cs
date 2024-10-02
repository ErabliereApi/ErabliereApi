using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Donnees.Action.Post;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ErabliereApi.Services;
using static ErabliereApi.Services.AlerteHelper;

namespace ErabliereApi.Attributes;

/// <summary>
/// Classe qui permet de rechercher et lancer les alertes relier à une action.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class TriggerAlertAttribute : ActionFilterAttribute
{
    private Guid? _idErabliere;
    private PostDonnee? _donnee;

    /// <summary>
    /// Contructeur par initialisation.
    /// </summary>
    /// <param name="order">Ordre d'exectuion des action filter</param>
    public TriggerAlertAttribute(int order = int.MinValue)
    {
        Order = order;
    }

    /// <inheritdoc />
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var id = context.ActionArguments["id"]?.ToString() ?? throw new InvalidOperationException("Le paramètre Id est requis dans la route pour utiliser l'attribue 'TriggerAlert'.");

        _idErabliere = Guid.Parse(id);

        _donnee = context.ActionArguments.Values.Single(a => a?.GetType() == typeof(PostDonnee)) as PostDonnee;
    }

    /// <inheritdoc />
    public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        var result = await next();

        if (!result.Canceled)
        {
            try
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<TriggerAlertAttribute>>();
                var depot = context.HttpContext.RequestServices.GetRequiredService<ErabliereDbContext>();
                var emailConfig = context.HttpContext.RequestServices.GetRequiredService<IOptions<EmailConfig>>();
                var emailService = context.HttpContext.RequestServices.GetRequiredService<IEmailService>();
                var smsConfig = context.HttpContext.RequestServices.GetRequiredService<IOptions<SMSConfig>>();
                var smsService = context.HttpContext.RequestServices.GetRequiredService<ISmsService>();
                var alertes = await depot.Alertes.AsNoTracking().Where(a => a.IdErabliere == _idErabliere && a.IsEnable).ToArrayAsync();

                for (int i = 0; i < alertes.Length; i++)
                {
                    var alerte = alertes[i];

                    MaybeTriggerAlerte(alerte, logger, emailConfig, emailService, smsConfig, smsService);
                }
            }
            catch (Exception e)
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<TriggerAlertAttribute>>();

                logger.LogCritical(new EventId(92837485, "TriggerAlertAttribute.OnActionExecuted"), e, "Une erreur imprévue est survenu lors de l'execution de la fonction d'alertage.");
            }
        }
    }

    private void MaybeTriggerAlerte(Alerte alerte, 
        ILogger<TriggerAlertAttribute> logger, 
        IOptions<EmailConfig> emailConfig, 
        IEmailService emailService,
        IOptions<SMSConfig> smsConfig,
        ISmsService smsService)
    {
        if (_donnee == null)
        {
            throw new InvalidOperationException("La donnée membre '_donnee' doit être initialiser pour utiliser la fonction d'alertage.");
        }

        var validationCount = 0;
        var conditionMet = 0;

        if (alerte.NiveauBassinThresholdHight != null && short.TryParse(alerte.NiveauBassinThresholdHight, out short nbth))
        {
            validationCount++;

            if (nbth > _donnee.NB)
            {
                conditionMet++;
            }
        }

        if (alerte.NiveauBassinThresholdLow != null && short.TryParse(alerte.NiveauBassinThresholdLow, out short nbtl))
        {
            validationCount++;

            if (nbtl < _donnee.NB)
            {
                conditionMet++;
            }
        }

        if (alerte.VacciumThresholdHight != null && short.TryParse(alerte.VacciumThresholdHight, out short vth))
        {
            validationCount++;

            if (vth > _donnee.V)
            {
                conditionMet++;
            }
        }

        if (alerte.VacciumThresholdLow != null && short.TryParse(alerte.VacciumThresholdLow, out short vtl))
        {
            validationCount++;

            if (vtl < _donnee.V)
            {
                conditionMet++;
            }
        }

        if (alerte.TemperatureThresholdHight != null && short.TryParse(alerte.TemperatureThresholdHight, out short tth))
        {
            validationCount++;

            if (tth > _donnee.T)
            {
                conditionMet++;
            }
        }

        if (alerte.TemperatureThresholdLow != null && short.TryParse(alerte.TemperatureThresholdLow, out short ttl))
        {
            validationCount++;

            if (ttl < _donnee.T)
            {
                conditionMet++;
            }
        }

        if (validationCount > 0 && validationCount == conditionMet)
        {
            Task.Run(() => TriggerAlerteCourriel(alerte, logger, emailConfig.Value, emailService, _donnee));
            Task.Run(() => TriggerAlerteSMS(alerte, logger, smsConfig.Value, smsService, _donnee));
        }
    }
}
