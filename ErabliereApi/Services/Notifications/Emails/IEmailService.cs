using MimeKit;

namespace ErabliereApi.Services;

/// <summary>
/// Interface permettant d'abstraire l'envoie des email
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Envoie un courriel
    /// </summary>
    /// <param name="mimeMessage"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task SendEmailAsync(MimeMessage mimeMessage, CancellationToken token);

    /// <summary>
    /// Envoie un courriel
    /// </summary>
    /// <param name="messageText"></param>
    /// <param name="recipient"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task SendEmailAsync(string messageText, string recipient, CancellationToken token);
}
