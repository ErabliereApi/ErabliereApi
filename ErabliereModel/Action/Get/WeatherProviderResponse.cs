namespace ErabliereApi.Donnees.Action.Get;

/// <summary>
/// Réponse de l'appel WeatherForecase/Provider
/// </summary>
public class WeatherProviderResponse
{
    /// <summary>
    /// Le nom du fournisseur de données météo
    /// </summary>
    public string Provider { get; set; } = string.Empty;
}
