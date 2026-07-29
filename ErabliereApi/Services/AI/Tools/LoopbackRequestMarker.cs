namespace ErabliereApi.Services.AI.Tools;

/// <summary>
/// Marque les requêtes qu'ErabliereAPI s'adresse à elle-même, et permet de les
/// reconnaître à l'arrivée.
/// </summary>
/// <remarks>
/// Les outils d'ErabliereAI rappellent l'API avec les identifiants de l'appelant :
/// une requête imbriquée est donc servie pendant qu'une autre est en cours. Tout
/// ce qui sérialise les requêtes entre elles doit laisser passer l'imbriquée, sinon
/// l'externe attend l'interne qui attend l'externe.
/// <para>
/// Le jeton est tiré au démarrage et ne quitte jamais le processus, donc un appelant
/// extérieur ne peut pas se faire passer pour une requête interne. Un simple nom
/// d'en-tête, lui, aurait suffi à contourner la sérialisation depuis l'extérieur.
/// </para>
/// </remarks>
public class LoopbackRequestMarker
{
    /// <summary>
    /// En-tête portant le jeton.
    /// </summary>
    public const string HeaderName = "X-ErabliereApi-Loopback";

    private readonly string _token = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Marque une requête sortante que l'API adresse à elle-même.
    /// </summary>
    public void Mark(HttpRequestMessage request)
    {
        // Retiré d'abord : un réessai rejoue le même message de requête.
        request.Headers.Remove(HeaderName);
        request.Headers.TryAddWithoutValidation(HeaderName, _token);
    }

    /// <summary>
    /// Indique si la requête entrante vient de ce processus.
    /// </summary>
    public bool IsLoopback(HttpRequest request)
    {
        return request.Headers.TryGetValue(HeaderName, out var token) &&
               string.Equals(token.ToString(), _token, StringComparison.Ordinal);
    }
}
