using System;

namespace ErabliereApi.Donnees;

/// <summary>
/// État d'un port d'un appareil
/// </summary>
public class PortEtat
{
    /// <summary>
    /// Identifiant unique de l'état du port
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// État du port (ex: open, closed, filtered)
    /// </summary>
    public string Etat { get; set; } = string.Empty;

    /// <summary>
    /// Raison de l'état (ex: connection refused, no response, etc.)
    /// </summary>
    public string Raison { get; set; } = string.Empty;

    /// <summary>
    /// Temps To de l'état du port
    /// </summary>
    public string RaisonTTL { get; set; } = string.Empty;

    /// <summary>
    /// Identifiant du port de l'appareil
    /// </summary>
    public Guid IdPortAppareil { get; set; }
}