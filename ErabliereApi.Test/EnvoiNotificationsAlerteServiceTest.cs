using ErabliereApi.Services;
using ErabliereApi.Services.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MimeKit;
using NSubstitute;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ErabliereApi.Test;

public class EnvoiNotificationsAlerteServiceTest
{
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly ISmsService _smsService = Substitute.For<ISmsService>();
    private readonly FileNotificationsAlerte _file = new();
    private readonly EnvoiNotificationsAlerteService _service;

    private static readonly NotificationAlerte Notification = new(
        Guid.NewGuid(),
        "Début d'alerte : Panne de courant - Secteur",
        "DÉBUT D'ALERTE : Panne de courant",
        "a@test.com; b@test.com;",
        "+15555550100;+15555550101");

    public EnvoiNotificationsAlerteServiceTest()
    {
        EnvoiNotificationsAlerteService.DelaisReprise = [TimeSpan.Zero, TimeSpan.Zero];

        var services = new ServiceCollection();
        services.AddSingleton(_emailService);
        services.AddSingleton(_smsService);
        services.Configure<EmailConfig>(c =>
        {
            c.Email = "alerte@test.com";
            c.Sender = "alerte@test.com";
        });
        services.Configure<SMSConfig>(c =>
        {
            c.Numero = "+15555550199";
            c.AccountSid = "sid";
            c.AuthToken = "token";
        });

        _service = new EnvoiNotificationsAlerteService(
            _file,
            services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
            NullLogger<EnvoiNotificationsAlerteService>.Instance);
    }

    [Fact]
    public async Task Envoyer_UnCourrielATousLesDestinatairesEtUnTextoParNumero()
    {
        MimeMessage? courriel = null;
        await _emailService.SendEmailAsync(Arg.Do<MimeMessage>(m => courriel = m), Arg.Any<CancellationToken>());

        await _service.EnvoyerAsync(Notification, CancellationToken.None);

        Assert.NotNull(courriel);
        Assert.Equal(Notification.Sujet, courriel.Subject);
        Assert.Equal(Notification.Message, courriel.TextBody);
        Assert.Equal(["a@test.com", "b@test.com"], courriel.To.Mailboxes.Select(m => m.Address));
        await _smsService.Received(1).SendSMSAsync(Notification.Message, "+15555550100", Arg.Any<CancellationToken>());
        await _smsService.Received(1).SendSMSAsync(Notification.Message, "+15555550101", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EchecPassager_EstRepris_SansRenvoyerLeTexto()
    {
        var essais = 0;
        _emailService.SendEmailAsync(Arg.Any<MimeMessage>(), Arg.Any<CancellationToken>())
                     .Returns(_ => ++essais == 1 ? Task.FromException(new TimeoutException()) : Task.CompletedTask);

        await _service.EnvoyerAsync(Notification, CancellationToken.None);

        Assert.Equal(2, essais);
        await _smsService.Received(2).SendSMSAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EchecPersistant_EstAbandonneApresTroisEssais_EtLeTextoPartQuandMeme()
    {
        _emailService.SendEmailAsync(Arg.Any<MimeMessage>(), Arg.Any<CancellationToken>())
                     .Returns(_ => Task.FromException(new TimeoutException()));

        await _service.EnvoyerAsync(Notification, CancellationToken.None);

        await _emailService.Received(3).SendEmailAsync(Arg.Any<MimeMessage>(), Arg.Any<CancellationToken>());
        await _smsService.Received(2).SendSMSAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ServiceDArrierePlan_VideLaFile()
    {
        await _service.StartAsync(CancellationToken.None);

        _file.Ajouter(Notification);
        _file.Ajouter(Notification);

        for (var i = 0; i < 200 && _file.EnAttente > 0; i++)
        {
            await Task.Delay(25);
        }

        await _service.StopAsync(CancellationToken.None);

        Assert.Equal(0, _file.EnAttente);
        await _emailService.Received(2).SendEmailAsync(Arg.Any<MimeMessage>(), Arg.Any<CancellationToken>());
    }
}
