using System;

namespace ErabliereApi.Donnees;

/// <summary>
/// Nom des host de l'appareil
/// </summary>
public class NomHostAppareil
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Guid IdAppareil { get; set; }
}