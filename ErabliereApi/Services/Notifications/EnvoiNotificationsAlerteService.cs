using Microsoft.Extensions.Options;
using MimeKit;

namespace ErabliereApi.Services.Notifications;

/// <summary>
/// Service d'arrière-plan qui envoie par courriel et par texto les messages déposés dans
/// <see cref="FileNotificationsAlerte"/>.
/// </summary>
public class EnvoiNotificationsAlerteService : BackgroundService
{
    /// <summary>
    /// Attente avant chaque nouvel essai d'un envoi échoué. Un message n'étant envoyé qu'une
    /// fois par changement d'état, un échec passager ne doit pas le faire perdre.
    /// </summary>
    public static TimeSpan[] DelaisReprise { get; set; } = [TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10)];

    private readonly FileNotificationsAlerte _file;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EnvoiNotificationsAlerteService> _logger;

    /// <summary>
    /// Constructeur par initialisation
    /// </summary>
    public EnvoiNotificationsAlerteService(
        FileNotificationsAlerte file,
        IServiceScopeFactory scopeFactory,
        ILogger<EnvoiNotificationsAlerteService> logger)
    {
        _file = file;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var notification in _file.LireAsync(stoppingToken))
            {
                try
                {
                    await EnvoyerAsync(notification, stoppingToken);
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    _logger.LogCritical(e, "Une erreur imprévue est survenu lors de l'envoie de l'alerte {AlerteId}.", notification.IdAlerte);
                }
                finally
                {
                    _file.MarquerTraitee();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Arrêt de l'application
        }
    }

    /// <summary>
    /// Envoie un message à tous ses destinataires.
    /// </summary>
    public async Task EnvoyerAsync(NotificationAlerte notification, CancellationToken token)
    {
        using var scope = _scopeFactory.CreateScope();

        await EnvoyerCourrielAsync(scope.ServiceProvider, notification, token);
        await EnvoyerSMSAsync(scope.ServiceProvider, notification, token);
    }

    private async Task EnvoyerCourrielAsync(IServiceProvider services, NotificationAlerte notification, CancellationToken token)
    {
        var destinataires = Destinataires(notification.EnvoyerA);

        if (destinataires.Length == 0)
        {
            return;
        }

        var emailConfig = services.GetRequiredService<IOptions<EmailConfig>>().Value;

        if (!emailConfig.IsConfigured || emailConfig.Sender == null)
        {
            _logger.LogWarning("Les configurations ne courriel ne sont pas initialisé, la fonctionnalité d'alerte ne peut pas fonctionner.");

            return;
        }

        var mailMessage = new MimeMessage();
        mailMessage.From.Add(new MailboxAddress("ErabliereAPI - Alerte Service", emailConfig.Sender));
        foreach (var destinataire in destinataires)
        {
            mailMessage.To.Add(MailboxAddress.Parse(destinataire));
        }
        mailMessage.Subject = notification.Sujet;
        mailMessage.Body = new TextPart("plain")
        {
            Text = notification.Message
        };

        var emailService = services.GetRequiredService<IEmailService>();

        await EssayerAsync(
            () => emailService.SendEmailAsync(mailMessage, token),
            "courriel", notification.IdAlerte, token);
    }

    private async Task EnvoyerSMSAsync(IServiceProvider services, NotificationAlerte notification, CancellationToken token)
    {
        var destinataires = Destinataires(notification.TexterA);

        if (destinataires.Length == 0)
        {
            return;
        }

        var smsConfig = services.GetRequiredService<IOptions<SMSConfig>>().Value;

        if (!smsConfig.IsConfigured)
        {
            _logger.LogWarning("Les configurations de SMS ne sont pas initialisé, la fonctionnalité d'alerte ne peut pas fonctionner.");

            return;
        }

        var smsService = services.GetRequiredService<ISmsService>();

        foreach (var destinataire in destinataires)
        {
            await EssayerAsync(
                () => smsService.SendSMSAsync(notification.Message, destinataire, token),
                "texto", notification.IdAlerte, token);
        }
    }

    private async Task EssayerAsync(Func<Task> envoi, string canal, Guid? idAlerte, CancellationToken token)
    {
        for (var essai = 0; ; essai++)
        {
            try
            {
                await envoi();

                return;
            }
            catch (Exception e) when (e is not OperationCanceledException && essai < DelaisReprise.Length)
            {
                _logger.LogWarning(e, "Échec de l'envoi du {Canal} de l'alerte {AlerteId}, essai {Essai}. Nouvel essai à venir.", canal, idAlerte, essai + 1);

                await Task.Delay(DelaisReprise[essai], token);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                _logger.LogCritical(e, "Abandon de l'envoi du {Canal} de l'alerte {AlerteId} après {Essais} essais.", canal, idAlerte, essai + 1);

                return;
            }
        }
    }

    private static string[] Destinataires(string? liste)
    {
        return liste?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
    }
}
