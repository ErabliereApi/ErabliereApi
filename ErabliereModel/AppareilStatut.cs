using System;

namespace ErabliereApi.Donnees;

/// <summary>
/// Statut d'un appareil
/// </summary>
public class AppareilStatut
{
    public Guid Id { get; set; }

    public string Etat { get; set; } = string.Empty;

    public string Raison { get; set; } = string.Empty;

    public string RaisonTTL { get; set; } = string.Empty;

    public Guid IdAppareil { get; set; }
}