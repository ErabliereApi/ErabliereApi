﻿using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Donnees.Action.Post;
using Microsoft.AspNetCore.Mvc.Filters;
using MimeKit;
using System.Text;
using System.Text.Json;
using static System.Text.Json.JsonSerializer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ErabliereApi.Services;

namespace ErabliereApi.Attributes;

/// <summary>
/// Classe qui permet de rechercher et lancer les alertes relier à une action.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class TriggerAlertV2Attribute : ActionFilterAttribute
{
    /// <summary>
    /// Contructeur par initialisation.
    /// </summary>
    /// <param name="order">Ordre d'exectuion des action filter</param>
    public TriggerAlertV2Attribute(int order = int.MinValue)
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
            throw new InvalidOperationException("Le paramètre Id est requis dans la route pour utiliser l'attribue 'TriggerAlertV2'.");

        var _idCapteur = Guid.Parse(id);

        try
        {
            var _donnee = context.ActionArguments.Values.Single(a => a?.GetType() == typeof(PostDonneeCapteur)) as PostDonneeCapteur;

            context.HttpContext.Items.Add("TriggerAlertV2Attribute", (_idCapteur, _donnee));
        }
        catch (InvalidOperationException e)
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<TriggerAlertV2Attribute>>();

            logger.LogCritical(92837485, e, "typeof(PostDonneeCapteur) not found in {0}", Serialize(context.ActionArguments.Where(a => a.Value?.GetType() != typeof(CancellationToken))));

            throw new InvalidOperationException("Le paramètre PostDonneeCapteur est requis dans la route pour utiliser l'attribue 'TriggerAlertV2'.");
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
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<TriggerAlertV2Attribute>>();
                var depot = context.HttpContext.RequestServices.GetRequiredService<ErabliereDbContext>();
                var emailConfig = context.HttpContext.RequestServices.GetRequiredService<IOptions<EmailConfig>>();
                var emailService = context.HttpContext.RequestServices.GetRequiredService<IEmailService>();
                var smsConfig = context.HttpContext.RequestServices.GetRequiredService<IOptions<SMSConfig>>();
                var smsService = context.HttpContext.RequestServices.GetRequiredService<ISmsService>();                

                var contextTupple = context.HttpContext.Items["TriggerAlertV2Attribute"];

                if (contextTupple == null)
                {
                    throw new InvalidOperationException("Les informations de l'attribue 'TriggerAlertV2' n'ont pas été trouvé dans le contexte.");
                }

                var (_idCapteur, _donnee) = ((Guid, PostDonneeCapteur?))contextTupple;

                var alertes = await depot.AlerteCapteurs.AsNoTracking()
                                                        .Where(a => a.IdCapteur == _idCapteur && a.IsEnable)
                                                        .ToArrayAsync();

                for (int i = 0; i < alertes.Length; i++)
                {
                    var alerte = alertes[i];

                    MaybeTriggerAlerte(
                        alerte, 
                        logger, 
                        emailConfig.Value, 
                        emailService, 
                        smsConfig.Value, 
                        smsService,
                        _donnee);
                }
            }
            catch (Exception e)
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<TriggerAlertV2Attribute>>();

                logger.LogCritical(new EventId(92837485, "TriggerAlertV2Attribute.OnActionExecuted"), e, "Une erreur imprévue est survenu lors de l'execution de la fonction d'alertage.");
            }
        }
    }

    private void MaybeTriggerAlerte(
        AlerteCapteur alerte, 
        ILogger<TriggerAlertV2Attribute> logger, 
        EmailConfig emailConfig, 
        IEmailService emailService, 
        SMSConfig smsConfig, 
        ISmsService smsService,
        PostDonneeCapteur? _donnee)
    {
        if (_donnee == null)
        {
            throw new InvalidOperationException("La donnée membre '_donnee' doit être initialiser pour utiliser la fonction d'alertage.");
        }

        var ajustedValue = _donnee.V / 10m;

        var validationCount = 0;
        var conditionMet = 0;

        if (alerte.MinVaue.HasValue)
        {
            validationCount++;

            if (ajustedValue <= alerte.MinVaue.Value)
            {
                conditionMet++;
            }
        }

        if (alerte.MaxValue.HasValue)
        {
            validationCount++;

            if (ajustedValue >= alerte.MaxValue.Value)
            {
                conditionMet++;
            }
        }

        if (conditionMet > 0)
        {
            TriggerAlerteCourriel(alerte, logger, emailConfig, emailService, _donnee);
            TriggerAlerteSMS(alerte, logger, smsConfig, smsService, _donnee);
        }
        else
        {
            logger.LogInformation("Alerte {AlerteId} {AlerteNom} not trigger", alerte.Id, alerte.Nom);
            logger.LogInformation("Validation count greater that 0 {ValidationCountGt0} && validation count eqal conditionMet {ValidationCount} == {ConditionMet} = false", validationCount > 0, validationCount, conditionMet);
        }
    }

    private async void TriggerAlerteCourriel(
        AlerteCapteur alerte, 
        ILogger<TriggerAlertV2Attribute> logger, 
        EmailConfig emailConfig, 
        IEmailService emailService,
        PostDonneeCapteur? _donnee)
    {
        if (!emailConfig.IsConfigured)
        {
            logger.LogWarning("Les configurations ne courriel ne sont pas initialisé, la fonctionnalité d'alerte ne peut pas fonctionner.");

            return;
        }

        try
        {
            if (alerte.EnvoyerA != null)
            {
                var mailMessage = new MimeMessage();
                mailMessage.From.Add(new MailboxAddress("ErabliereAPI - Alerte Service", emailConfig.Sender));
                foreach (var destinataire in alerte.EnvoyerA.Split(';'))
                {
                    mailMessage.To.Add(MailboxAddress.Parse(destinataire));
                }
                mailMessage.Subject = $"Alerte ID : {alerte.Id}";
                mailMessage.Body = new TextPart("plain")
                {
                    Text = FormatTextMessage(alerte, _donnee)
                };

                await emailService.SendEmailAsync(mailMessage, CancellationToken.None);
            }
        }
        catch (Exception e)
        {
            logger.LogCritical(new EventId(92837486, "TriggerAlertV2Attribute.TriggerAlerte"), e, "Une erreur imprévue est survenu lors de l'envoie de l'alerte.");
        }
    }

    private static async void TriggerAlerteSMS(
        AlerteCapteur alerte, 
        ILogger<TriggerAlertV2Attribute> logger, 
        SMSConfig smsConfig, 
        ISmsService smsService,
        PostDonneeCapteur? _donnee)
    {
        if (!smsConfig.IsConfigured)
        {
            logger.LogWarning("Les configurations de SMS ne sont pas initialisé, la fonctionnalité d'alerte ne peut pas fonctionner.");

            return;
        }

        try
        {
            if (alerte.TexterA != null)
            {
                var message = FormatTextMessage(alerte, _donnee);

                foreach (var destinataire in alerte.TexterA.Split(';'))
                {
                    await smsService.SendSMSAsync(message, destinataire, CancellationToken.None);
                }
            }
        }
        catch (Exception e)
        {
            logger.LogCritical(new EventId(92837486, "TriggerAlertV2Attribute.TriggerAlerte"), e, "Une erreur imprévue est survenu lors de l'envoie de l'alerte.");
        }
    }

    private static string FormatTextMessage(AlerteCapteur alerte, PostDonneeCapteur? donnee)
    {
        var sb = new StringBuilder();

        sb.Append(nameof(AlerteCapteur));
        sb.AppendLine(" : ");
        sb.AppendLine(Serialize(alerte, _mailSerializerSettings));
        sb.AppendLine();
        sb.Append(nameof(PostDonneeCapteur));
        sb.AppendLine(" : ");
        sb.AppendLine(Serialize(donnee, _mailSerializerSettings));
        sb.AppendLine();
        sb.AppendLine($"Date : {DateTimeOffset.UtcNow}");

        return sb.ToString();
    }

    private readonly static JsonSerializerOptions _mailSerializerSettings = new() { WriteIndented = true };
}
