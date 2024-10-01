namespace ErabliereApi.Donnees.Interfaces;

/// <summary>
/// Interface pour les objets qui peuvent être public
/// </summary>
public interface IIsPublic
{
    /// <summary>
    /// Indique si l'objet est public
    /// </summary>
    public bool IsPublic { get; set; }
}
