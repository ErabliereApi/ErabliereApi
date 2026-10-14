namespace ErabliereApi.Services.Notifications;

/// <summary>
/// Changement d'état d'une alerte de capteur causé par une donnée.
/// </summary>
public enum TypeTransitionAlerte
{
    /// <summary>
    /// L'état de l'alerte ne change pas, aucun message à envoyer.
    /// </summary>
    Aucune,

    /// <summary>
    /// L'alerte commence.
    /// </summary>
    Debut,

    /// <summary>
    /// L'alerte se termine.
    /// </summary>
    Fin
}
