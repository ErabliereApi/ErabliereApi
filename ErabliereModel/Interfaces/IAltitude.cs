namespace ErabliereApi.Donnees.Interfaces;

/// <summary>
/// Interface pour les classes possédant une altitude
/// </summary>
public interface IAltitude
{
    /// <summary>
    /// Base
    /// </summary>
    public double? Base { get; set; }

    /// <summary>
    /// Sommet
    /// </summary>
    public double? Sommet { get; set; }
}
