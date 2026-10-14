using ErabliereApi.Donnees;

namespace ErabliereApi.Services.Notifications;

/// <summary>
/// Détermine si une donnée fait changer l'état d'une alerte de capteur.
/// </summary>
public static class AlerteCapteurTransition
{
    /// <summary>
    /// Évalue le changement d'état causé par une donnée, sans modifier l'alerte.
    /// </summary>
    /// <param name="alerte">L'alerte et son état courant</param>
    /// <param name="valeur">La valeur de la donnée</param>
    /// <param name="date">La date de la donnée, qui fait foi plutôt que l'heure de réception</param>
    public static TypeTransitionAlerte Evaluer(AlerteCapteur alerte, decimal valeur, DateTimeOffset date)
    {
        // Une donnée envoyée en différé ne renverse pas un changement d'état plus récent qu'elle.
        if (alerte.DateChangementEtat.HasValue && date <= alerte.DateChangementEtat.Value)
        {
            return TypeTransitionAlerte.Aucune;
        }

        if (!alerte.EstEnAlerte)
        {
            return EstHorsSeuil(alerte, valeur) ? TypeTransitionAlerte.Debut : TypeTransitionAlerte.Aucune;
        }

        return EstRevenuALaNormale(alerte, valeur) ? TypeTransitionAlerte.Fin : TypeTransitionAlerte.Aucune;
    }

    private static bool EstHorsSeuil(AlerteCapteur alerte, decimal valeur)
    {
        return (alerte.MinValue.HasValue && valeur <= alerte.MinValue.Value) ||
               (alerte.MaxValue.HasValue && valeur >= alerte.MaxValue.Value);
    }

    private static bool EstRevenuALaNormale(AlerteCapteur alerte, decimal valeur)
    {
        var marge = Math.Max(alerte.Marge ?? 0, 0);

        return (!alerte.MinValue.HasValue || valeur > alerte.MinValue.Value + marge) &&
               (!alerte.MaxValue.HasValue || valeur < alerte.MaxValue.Value - marge);
    }
}
