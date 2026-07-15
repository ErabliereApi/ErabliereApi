using System;

namespace ErabliereApi.Donnees;

/// <summary>
/// Les fréquences de facturation connues pour un abonnement payant.
/// Contrairement aux clés d'API facturées à l'utilisation, les abonnements
/// de compte utilisateur sont facturés à fréquence fixe.
/// </summary>
public static class FrequencesFacturation
{
    /// <summary>
    /// Facturation mensuelle (16 $/mois). Correspond au prix Stripe configuré
    /// par la clé de configuration 'Stripe.AbonnementMensuelPriceId'.
    /// </summary>
    public const string Mensuelle = "mensuelle";

    /// <summary>
    /// Facturation annuelle (166 $/an). Correspond au prix Stripe configuré
    /// par la clé de configuration 'Stripe.AbonnementAnnuelPriceId'.
    /// </summary>
    public const string Annuelle = "annuelle";

    /// <summary>
    /// La liste des fréquences de facturation valides.
    /// </summary>
    public static readonly string[] Toutes = [Mensuelle, Annuelle];

    /// <summary>
    /// Indique si la fréquence de facturation reçue est une fréquence connue.
    /// </summary>
    public static bool EstValide(string? frequence)
    {
        return frequence != null && Array.Exists(Toutes, f => string.Equals(f, frequence, StringComparison.OrdinalIgnoreCase));
    }
}
