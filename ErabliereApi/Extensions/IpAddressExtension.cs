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
        return IPAddress.IsLoopback(ipAddress) ||
               ipAddress.IsIPv6LinkLocal ||
               ipAddress.IsIPv6SiteLocal ||
               (ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && // IPv4
                (ipAddress.GetAddressBytes()[0] == 10 || // 10.0.0.0/8
                 (ipAddress.GetAddressBytes()[0] == 172 && ipAddress.GetAddressBytes()[1] >= 16 && ipAddress.GetAddressBytes()[1] <= 31) || // 172.16.0.0/12
                 (ipAddress.GetAddressBytes()[0] == 192 && ipAddress.GetAddressBytes()[1] == 168)) // 192.168.0.0/16
                );
    }
}