using System;

namespace ErabliereApi.Donnees.Interfaces;

/// <summary>
/// Interface pour les classes possédant des informations de dates
/// </summary>
public interface IDatesInfo
{
    /// <summary>
    /// Date de création
    /// </summary>
    public DateTimeOffset? DC { get; set; }
}