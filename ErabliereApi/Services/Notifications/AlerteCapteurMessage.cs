using ErabliereApi.Donnees;
using System.Globalization;
using System.Text;

namespace ErabliereApi.Services.Notifications;

/// <summary>
/// Rédige les messages de début et de fin d'alerte de capteur.
/// </summary>
public static class AlerteCapteurMessage
{
    /// <summary>
    /// Crée le message annonçant un changement d'état.
    /// </summary>
    /// <param name="alerte">L'alerte, avec son capteur et l'érablière du capteur si disponibles</param>
    /// <param name="transition">Début ou fin</param>
    /// <param name="valeur">La valeur de la donnée</param>
    /// <param name="date">La date de la donnée</param>
    /// <param name="debut">La date de début de l'alerte qui se termine, pour calculer la durée</param>
    public static NotificationAlerte Creer(
        AlerteCapteur alerte,
        TypeTransitionAlerte transition,
        decimal valeur,
        DateTimeOffset date,
        DateTimeOffset? debut)
    {
        var estDebut = transition == TypeTransitionAlerte.Debut;
        var nomAlerte = string.IsNullOrWhiteSpace(alerte.Nom) ? "Alerte sans nom" : alerte.Nom;
        var nomCapteur = string.IsNullOrWhiteSpace(alerte.Capteur?.Nom) ? alerte.IdCapteur?.ToString() : alerte.Capteur.Nom;
        var nomErabliere = alerte.Capteur?.Erabliere?.Nom;
        var symbole = alerte.Capteur?.Symbole;

        var sujet = new StringBuilder(estDebut ? "Début d'alerte : " : "Fin d'alerte : ");
        sujet.Append(nomAlerte).Append(" - ").Append(nomCapteur);
        if (!string.IsNullOrWhiteSpace(nomErabliere))
        {
            sujet.Append(" (").Append(nomErabliere).Append(')');
        }

        var sb = new StringBuilder();
        sb.Append(estDebut ? "DÉBUT D'ALERTE : " : "FIN D'ALERTE : ").AppendLine(nomAlerte);
        sb.Append("Capteur : ").AppendLine(nomCapteur);
        if (!string.IsNullOrWhiteSpace(nomErabliere))
        {
            sb.Append("Érablière : ").AppendLine(nomErabliere);
        }
        sb.Append("Valeur : ").Append(valeur.ToString("0.####", CultureInfo.InvariantCulture));
        if (!string.IsNullOrWhiteSpace(symbole))
        {
            sb.Append(' ').Append(symbole);
        }
        sb.AppendLine();
        sb.Append("Heure de la donnée : ").AppendLine(FormaterDate(date));

        if (!estDebut && debut.HasValue)
        {
            sb.Append("Début de l'alerte : ").AppendLine(FormaterDate(debut.Value));
            sb.Append("Durée : ").AppendLine(FormaterDuree(date - debut.Value));
        }

        return new NotificationAlerte(alerte.Id, sujet.ToString(), sb.ToString(), alerte.EnvoyerA, alerte.TexterA);
    }

    /// <summary>
    /// Formate une date dans le fuseau horaire du serveur, avec son décalage.
    /// </summary>
    public static string FormaterDate(DateTimeOffset date)
    {
        return date.ToLocalTime().ToString("yyyy-MM-dd HH:mm '(UTC'zzz')'", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Formate une durée en jours, heures et minutes.
    /// </summary>
    public static string FormaterDuree(TimeSpan duree)
    {
        if (duree < TimeSpan.FromMinutes(1))
        {
            return "moins d'une minute";
        }

        var parties = new List<string>(3);

        if (duree.Days > 0)
        {
            parties.Add($"{duree.Days} j");
        }
        if (duree.Hours > 0)
        {
            parties.Add($"{duree.Hours} h");
        }
        if (duree.Minutes > 0)
        {
            parties.Add($"{duree.Minutes} min");
        }

        return string.Join(' ', parties);
    }
}
