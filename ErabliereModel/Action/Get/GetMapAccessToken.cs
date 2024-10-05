namespace ErabliereApi.Donnees.Action.Get;

/// <summary>
/// Modèle d'obtention d'un access token pour une carte
/// </summary>
public class GetMapAccessToken
{
    /// <summary>
    /// L'access token
    /// </summary>
    public string? AccessToken { get; set; }
}