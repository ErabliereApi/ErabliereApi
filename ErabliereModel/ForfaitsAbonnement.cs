using System;

namespace ErabliereApi.Donnees;

/// <summary>
/// Les forfaits d'abonnement connus de l'application.
/// </summary>
public static class ForfaitsAbonnement
{
    /// <summary>
    /// Le forfait gratuit, aucun paiement requis.
    /// </summary>
    public const string Gratuit = "gratuit";

    /// <summary>
    /// Le forfait de base, payant. Contrairement aux clés d'API facturées à
    /// l'utilisation ('Stripe.BasePlanPriceId'), ce forfait est facturé à fréquence
    /// fixe selon la <see cref="FrequencesFacturation" /> choisie : 16 $/mois
    /// ('Stripe.AbonnementMensuelPriceId') ou 166 $/an ('Stripe.AbonnementAnnuelPriceId').
    /// </summary>
    public const string Base = "base";

    /// <summary>
    /// La liste des forfaits valides.
    /// </summary>
    public static readonly string[] Tous = [Gratuit, Base];

    /// <summary>
    /// Indique si le forfait reçu est un forfait connu.
    /// </summary>
    public static bool EstValide(string? forfait)
    {
        return forfait != null && Array.Exists(Tous, f => string.Equals(f, forfait, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Indique si le forfait nécessite un paiement.
    /// </summary>
    public static bool EstPayant(string? forfait)
    {
        return string.Equals(forfait, Base, StringComparison.OrdinalIgnoreCase);
    }
}
