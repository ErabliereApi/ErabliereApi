namespace ErabliereApi.Donnees.Interfaces;

/// <summary>
/// Interface pour les classes possédant une localisation
/// </summary>
public interface ILocalizable
{
    /// <summary>
    /// Latitude
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// Longitude
    /// </summary>
    public double Longitude { get; set; }
}
