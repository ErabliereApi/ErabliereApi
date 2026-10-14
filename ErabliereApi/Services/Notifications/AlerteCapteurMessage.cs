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
    /// Crée le message annonçant qu'un capteur est muet depuis plus longtemps que le délai de silence.
    /// </summary>
    /// <param name="alerte">L'alerte, avec son capteur et l'érablière du capteur si disponibles</param>
    /// <param name="depuis">L'heure de la dernière réception, ou du début de la surveillance si rien n'a été reçu</param>
    /// <param name="receptionConnue">Faux si aucune donnée n'a été reçue depuis le début de la surveillance</param>
    public static NotificationAlerte CreerSilence(AlerteCapteur alerte, DateTimeOffset depuis, bool receptionConnue)
    {
        var (sujet, sb) = Entete(alerte, "Capteur muet : ", "CAPTEUR MUET : ");

        // Le serveur ne voit que l'absence de données : il ne peut pas en connaître la cause.
        sb.Append(receptionConnue ? "Plus de nouvelles depuis " : "Aucune donnée reçue depuis le début de la surveillance, ")
          .Append(FormaterDate(depuis))
          .AppendLine(" : panne de courant ou perte de réseau.");
        sb.AppendLine("Le serveur ne peut pas distinguer les deux, ni un appareil débranché ou brisé.");
        if (alerte.DelaiSilenceMinutes.HasValue)
        {
            sb.Append("Délai de silence : ").AppendLine(FormaterDuree(TimeSpan.FromMinutes(alerte.DelaiSilenceMinutes.Value)));
        }

        return new NotificationAlerte(alerte.Id, sujet, sb.ToString(), alerte.EnvoyerA, alerte.TexterA);
    }

    /// <summary>
    /// Crée le message annonçant que le capteur muet envoie de nouveau des données.
    /// </summary>
    /// <param name="alerte">L'alerte, avec son capteur et l'érablière du capteur si disponibles</param>
    /// <param name="debut">L'heure de la dernière réception avant le silence</param>
    /// <param name="reprise">L'heure de réception de la première donnée après le silence</param>
    public static NotificationAlerte CreerReprise(AlerteCapteur alerte, DateTimeOffset debut, DateTimeOffset reprise)
    {
        var (sujet, sb) = Entete(alerte, "Données reprises : ", "DONNÉES REPRISES : ");

        sb.Append("Plus de nouvelles depuis : ").AppendLine(FormaterDate(debut));
        sb.Append("Reprise : ").AppendLine(FormaterDate(reprise));
        sb.Append("Durée du silence : ").AppendLine(FormaterDuree(reprise - debut));

        return new NotificationAlerte(alerte.Id, sujet, sb.ToString(), alerte.EnvoyerA, alerte.TexterA);
    }

    private static (string sujet, StringBuilder corps) Entete(AlerteCapteur alerte, string prefixeSujet, string prefixeCorps)
    {
        var nomAlerte = string.IsNullOrWhiteSpace(alerte.Nom) ? "Alerte sans nom" : alerte.Nom;
        var nomCapteur = string.IsNullOrWhiteSpace(alerte.Capteur?.Nom) ? alerte.IdCapteur?.ToString() : alerte.Capteur.Nom;
        var nomErabliere = alerte.Capteur?.Erabliere?.Nom;

        var sujet = new StringBuilder(prefixeSujet).Append(nomCapteur);
        if (!string.IsNullOrWhiteSpace(nomErabliere))
        {
            sujet.Append(" (").Append(nomErabliere).Append(')');
        }

        var sb = new StringBuilder();
        sb.Append(prefixeCorps).AppendLine(nomAlerte);
        sb.Append("Capteur : ").AppendLine(nomCapteur);
        if (!string.IsNullOrWhiteSpace(nomErabliere))
        {
            sb.Append("Érablière : ").AppendLine(nomErabliere);
        }

        return (sujet.ToString(), sb);
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
