using System.Net;

namespace ErabliereApi.Extensions;

/// <summary>
/// Extensions pour IPAddress.
/// </summary>
public static class IpAddressExtension
{
    /// <summary>
    /// Vérifie si une adresse IP est privée.
    /// </summary>
    /// <param name="ipAddress">L'adresse IP à vérifier.</param>
    /// <returns>True si l'adresse IP est privée, sinon false.</returns>
    public static bool IsPrivateIp(this IPAddress ipAddress)
    {
        var isLoopback = IPAddress.IsLoopback(ipAddress);
        var isIPv6LinkLocal = ipAddress.IsIPv6LinkLocal;
        var isIPv6SiteLocal = ipAddress.IsIPv6SiteLocal;
        var isIPv4Private = IsPrivateIpV4Range(ipAddress);

        var ipv6MappedToIPv4 = ipAddress.IsIPv4MappedToIPv6;
        if (ipv6MappedToIPv4)
        {
            var ipv4 = ipAddress.MapToIPv4();
            isIPv4Private = IsPrivateIpV4Range(ipv4);
        }

        return isLoopback ||
               isIPv6LinkLocal ||
               isIPv6SiteLocal ||
                isIPv4Private;
    }

    private static bool IsPrivateIpV4Range(IPAddress ipAddress)
    {
        return (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && // IPv4
            (ipAddress.GetAddressBytes()[0] == 10 || // 10.0.0.0/8
                (ipAddress.GetAddressBytes()[0] == 172 && ipAddress.GetAddressBytes()[1] >= 16 && ipAddress.GetAddressBytes()[1] <= 31) || // 172.16.0.0/12
                (ipAddress.GetAddressBytes()[0] == 192 && ipAddress.GetAddressBytes()[1] == 168)) // 192.168.0.0/16
            );
    }
}