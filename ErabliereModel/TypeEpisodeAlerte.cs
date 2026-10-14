using System.Text.Json.Serialization;

namespace ErabliereApi.Donnees;

/// <summary>
/// Ce qu'un <see cref="EpisodeAlerteCapteur"/> mesure
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TypeEpisodeAlerte
{
    /// <summary>
    /// Une valeur hors des seuils de l'alerte. Les dates sont celles des données.
    /// </summary>
    Seuil = 0,

    /// <summary>
    /// Aucune donnée reçue pendant plus que le délai de silence de l'alerte : panne de courant
    /// ou perte de réseau, le serveur ne peut pas distinguer les deux. Les dates sont des heures
    /// de réception.
    /// </summary>
    Silence = 1
}
