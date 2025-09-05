namespace ErabliereApi.Extensions;

/// <summary>
/// Extensions pour HttpContext.
/// </summary>
public static class HttpContextExtension
{
    /// <summary>
    /// Clé de l'entête HTTP pour l'adresse IP réelle du client.
    /// </summary>
    public const string RealIPKey = "X-Real-IP";

    /// <summary>
    /// Clé de l'entête HTTP pour l'adresse IP réelle du client.
    /// </summary>
    public const string ForwardedForKey = "X-Forwarded-For";

    /// <summary>
    /// Permet d'extraire l'id de l'applant.
    /// Si l'entête X-Real-IP ou X-Forwarded-For est présent, cette entête sera utilisé. 
    /// Sinon l'adresse ip sera retourner depuis la propriété RemoteIpAddress.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static string GetClientIp(this HttpContext context)
    {
        var value = ValiderEntente(context, RealIPKey);
        if (value != null)
        {
            return value;
        }
        value = ValiderEntente(context, ForwardedForKey);
        if (value != null)
        {
            return value;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "";
    }

    private static string? ValiderEntente(HttpContext context, string headerName)
    {
        if (context.Request.Headers.ContainsKey(headerName))
        {
            var ips = context.Request.Headers[headerName];

            if (ips.Count == 1)
            {
                return ips.ToString();
            }
            else
            {
                throw new InvalidOperationException($"Plusieurs entêtes {headerName} trouvé dans la requête.");
            }
        }

        return null;
    }
}