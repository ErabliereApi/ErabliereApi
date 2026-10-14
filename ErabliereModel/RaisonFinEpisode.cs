using System.Text.Json.Serialization;

namespace ErabliereApi.Donnees;

/// <summary>
/// La raison pour laquelle un <see cref="EpisodeAlerteCapteur"/> s'est terminé
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RaisonFinEpisode
{
    /// <summary>
    /// Une donnée est revenue à la normale. La date et la valeur de fin sont celles de cette donnée.
    /// </summary>
    RetourNormale = 0,

    /// <summary>
    /// Les seuils ou la marge de l'alerte ont été modifiés pendant l'épisode.
    /// La fin n'a pas été mesurée : la date est celle de la modification.
    /// </summary>
    AlerteModifiee = 1,

    /// <summary>
    /// L'alerte a été désactivée pendant l'épisode.
    /// La fin n'a pas été mesurée : la date est celle de la désactivation.
    /// </summary>
    AlerteDesactivee = 2,

    /// <summary>
    /// L'alerte a été supprimée pendant l'épisode.
    /// La fin n'a pas été mesurée : la date est celle de la suppression.
    /// </summary>
    AlerteSupprimee = 3
}
