using System;

namespace ErabliereApi.Donnees;

/// <summary>
/// Statut d'un appareil
/// </summary>
public class AppareilStatut
{
    /// <summary>
    /// Identifiant unique du statut
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// État actuel de l'appareil
    /// </summary>
    public string Etat { get; set; } = string.Empty;

    /// <summary>
    /// Raison de l'état
    /// </summary>
    public string Raison { get; set; } = string.Empty;

    /// <summary>
    /// Raison de l'état (time-to-live)
    /// </summary>
    public string RaisonTTL { get; set; } = string.Empty;

    /// <summary>
    /// Identifiant de l'appareil
    /// </summary>
    public Guid IdAppareil { get; set; }
}