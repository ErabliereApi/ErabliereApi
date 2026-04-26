namespace ErabliereApi.Extensions;

/// <summary>
/// Classe d'extension pour les réponses Http
/// </summary>
public static class HttpResponseExtension
{
    /// <summary>
    /// Valide si le statut de la répose est un succès et journalise de manière détaillé l'erreur
    /// </summary>
    /// <param name="response"></param>
    /// <param name="logger"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task EnsureSuccessStatusCodeAsync(
        this HttpResponseMessage response,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (response.IsSuccessStatusCode)
            return;

        string content = string.Empty;
        try
        {
            content = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read response content.");
        }

        logger.LogError(
            "HTTP request failed. StatusCode: {StatusCode}, Reason: {ReasonPhrase}, Content: {Content}",
            response.StatusCode, response.ReasonPhrase, content);

        response.EnsureSuccessStatusCode();
    }
}
