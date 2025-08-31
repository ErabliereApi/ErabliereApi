using System;

namespace ErabliereApi.Donnees;

/// <summary>
/// Information sur un port d'un appareil
/// </summary>
public class PortAppareil
{
    /// <summary>
    /// Clé unique du port
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Port de l'appareil
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Protocole du port (tcp, udp, etc.)
    /// </summary>
    public string Protocole { get; set; } = string.Empty;

    /// <summary>
    /// État du port
    /// </summary>
    public PortEtat? Etat { get; set; }

    /// <summary>
    /// Service du port
    /// </summary>
    public PortService? PortService { get; set; }

    /// <summary>
    /// Id de l'appareil
    /// </summary>
    public Guid IdAppareil { get; set; }
}