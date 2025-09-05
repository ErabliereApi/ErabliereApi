using System.Net;

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
        var remoteIp = context.Connection.RemoteIpAddress;

        if (remoteIp == null || remoteIp.IsPrivateIp())
        {
            var realIp = ValiderEntente(context, RealIPKey);
            var forwardedFor = ValiderEntente(context, ForwardedForKey);
            
            if (realIp != null && forwardedFor != null)
            {
                throw new InvalidOperationException($"Les deux entêtes {RealIPKey} et {ForwardedForKey} sont présents dans la requête.");
            }

            remoteIp = realIp ?? forwardedFor ?? remoteIp;
        }

        return remoteIp?.ToString() ?? "";
    }

    private static IPAddress? ValiderEntente(HttpContext context, string headerName)
    {
        if (context.Request.Headers.ContainsKey(headerName))
        {
            var ips = context.Request.Headers[headerName];

            if (ips.Count == 1 && IPAddress.TryParse(ips.ToString(), out var iPAddress))
            {
                return iPAddress;
            }
            else
            {
                throw new InvalidOperationException($"Plusieurs entêtes {headerName} trouvé dans la requête.");
            }
        }

        return null;
    }
}