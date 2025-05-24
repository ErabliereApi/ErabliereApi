namespace ErabliereApi.Donnees.Action.Put;

/// <summary>
/// Représente une requête pour mettre à jour le nom d'une clé d'API.
/// </summary>
public class PutApiKeyName
{
    /// <summary>
    /// Nom de la clé d'API.
    /// </summary>
    public string? Name { get; set; }
}