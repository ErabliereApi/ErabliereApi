using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace ErabliereApi.Services.SMS;

/// <summary>
/// Implementation of <see cref="ISmsService" /> using Twilio
/// </summary>
public class TwilioSmsService : ISmsService
{
    private readonly SMSConfig _smsConfig;
    private readonly ILogger<TwilioSmsService> _logger;

    /// <summary>
    /// Constructeur par défaut
    /// </summary>
    /// <param name="smsConfig"></param>
    /// <param name="logger"></param>
    public TwilioSmsService(
        IOptions<SMSConfig> smsConfig,
        ILogger<TwilioSmsService> logger)
    {
        _smsConfig = smsConfig.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SendSMSAsync(string message, string destinataire, CancellationToken token)
    {
        try
        {
            _logger.LogInformation("Begin sending SMS...");

            string? numero = _smsConfig.Numero;
            string? accountSid = _smsConfig.AccountSid;
            string? authToken = _smsConfig.AuthToken;

            TwilioClient.Init(accountSid, authToken);

            await MessageResource.CreateAsync(
                body: message,
                from: new PhoneNumber(numero),
                to: new PhoneNumber(destinataire)
            );

            _logger.LogInformation("SMS sent successfully");
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, e.Message);
        }
    }
}
