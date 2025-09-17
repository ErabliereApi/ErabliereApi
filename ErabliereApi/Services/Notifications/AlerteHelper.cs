using ErabliereApi.Donnees;
using MimeKit;
using System.Text;
using ErabliereApi.Donnees.Action.Post;
using ErabliereModel.Interfaces;

namespace ErabliereApi.Services;

/// <summary>
/// Collection of helper methods for the Alerte service.
/// </summary>
public static class AlerteHelper
{
    const string ErrorMessage = "Une erreur imprévue est survenu lors de l'envoie de l'alerte.";

    /// <summary>
    /// Lancer une alerte par courriel pour une alerte du trio de données.
    /// </summary>
    /// <param name="alerte"></param>
    /// <param name="logger"></param>
    /// <param name="emailConfig"></param>
    /// <param name="emailService"></param>
    /// <param name="_donnee"></param>
    /// <returns></returns>
    public static async Task TriggerAlerteCourriel(
        Alerte alerte, ILogger logger, EmailConfig emailConfig, IEmailService emailService, PostDonnee _donnee)
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
            logger.LogCritical(new EventId(92837486, "TriggerAlertAttribute.TriggerAlerte"), e, ErrorMessage);
        }
    }

    /// <summary>
    /// Lancer une alerte par courriel pour une alerte d'un capteur.
    /// </summary>
    public static async Task TriggerAlerteCourriel(
        AlerteCapteur alerte,
        ILogger logger,
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
            logger.LogCritical(new EventId(92837486, "AlerteHelper.TriggerAlerteCourriel PostDonneeCapteur"), e, ErrorMessage);
        }
    }

    /// <summary>
    /// Lancer une alerte par courriel pour une alerte d'un capteur (v2).
    /// </summary>
    /// <param name="alerte"></param>
    /// <param name="logger"></param>
    /// <param name="emailConfig"></param>
    /// <param name="emailService"></param>
    /// <param name="_donnee"></param>
    /// <returns></returns>
    public static async Task TriggerAlerteCourriel(
        AlerteCapteur alerte,
        ILogger logger,
        EmailConfig emailConfig,
        IEmailService emailService,
        PostDonneeCapteurV2? _donnee)
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
            logger.LogCritical(new EventId(92837486, "TriggerAlertV3Attribute.TriggerAlerte"), e, ErrorMessage);
        }
    }

    /// <summary>
    /// Lancer une alerte par SMS pour une alerte du trio de données.
    /// </summary>
    public static async Task TriggerAlerteSMS(
        Alerte alerte, ILogger logger, SMSConfig smsConfig, ISmsService smsService, PostDonnee _donnee)
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
            logger.LogCritical(new EventId(92837486, "TriggerAlertAttribute.TriggerAlerte"), e, ErrorMessage);
        }
    }

    /// <summary>
    /// Lancer une alerte par SMS pour une alerte d'un capteur.
    /// </summary>
    public static async Task TriggerAlerteSMS(
        AlerteCapteur alerte,
        ILogger logger,
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
            logger.LogCritical(new EventId(92837486, "TriggerAlertV2Attribute.TriggerAlerte"), e, ErrorMessage);
        }
    }

    /// <summary>
    /// Lancer une alerte par SMS pour une alerte d'un capteur (v2).
    /// </summary>
    /// <param name="alerte"></param>
    /// <param name="logger"></param>
    /// <param name="smsConfig"></param>
    /// <param name="smsService"></param>
    /// <param name="_donnee"></param>
    /// <returns></returns>
    public static async Task TriggerAlerteSMS(
        AlerteCapteur alerte,
        ILogger logger,
        SMSConfig smsConfig,
        ISmsService smsService,
        PostDonneeCapteurV2? _donnee)
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
            logger.LogCritical(new EventId(92837486, "TriggerAlertV3Attribute.TriggerAlerte"), e, ErrorMessage);
        }
    }

    private static string FormatTextMessage(IAlerteTexte alerte, IDonneeTexte? donnee)
    {
        var sb = new StringBuilder();

        sb.AppendLine("ErabliereAPI - Alerte");
        sb.AppendLine(alerte.GetAlerteTexte() ?? "Aucune information d'alerte disponible.");
        sb.AppendLine();
        sb.AppendLine("Donnée reçu : ");
        sb.AppendLine(donnee?.GetDonneeTexte() ?? "Aucune donnée disponible.");
        sb.AppendLine();
        sb.AppendLine($"Date : {DateTimeOffset.UtcNow}");

        return sb.ToString();
    }
}