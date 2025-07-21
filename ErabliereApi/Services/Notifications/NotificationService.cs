using MimeKit;

namespace ErabliereApi.Services.Notifications;

/// <summary>
/// Enum representing the type of notification to be sent.
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// Represents a notification sent via SMS.
    /// </summary>
    Sms,

    /// <summary>
    /// Represents a notification sent via email.
    /// </summary>
    Email
}

/// <summary>
/// Service for sending notifications.
/// </summary>
public class NotificationService
{
    private readonly ISmsService _smsService;
    private readonly IEmailService _emailService;
    private readonly ILogger<NotificationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationService"/> class.
    /// /// </summary>
    public NotificationService(
        ISmsService smsService,
        IEmailService emailService,
        ILogger<NotificationService> logger)
    {
        _smsService = smsService;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Sends a notification message to the specified recipient.
    /// </summary>
    public async Task SendNotificationAsync(string message, string recipient, NotificationType type, CancellationToken token = default)
    {
        if (type == NotificationType.Sms)
        {
            try
            {
                await _smsService.SendSMSAsync(message, recipient, token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SMS notification to {Recipient}", recipient);
            }
            
        }
        if (type == NotificationType.Email)
        {
            try
            {
                await _emailService.SendEmailAsync(message, recipient, token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email notification to {Recipient}", recipient);
            }
        }
    }
}