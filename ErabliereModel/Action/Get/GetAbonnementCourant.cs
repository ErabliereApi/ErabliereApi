using System;

namespace ErabliereApi.Donnees.Action.Get;

/// <summary>
/// Le forfait courant de l'utilisateur authentifié.
///
/// Contrairement à <see cref="Abonnement" />, cette classe n'expose ni l'id Stripe
/// ni l'id du client : elle est faite pour être consommée par des composants
/// externes qui n'ont besoin que de savoir à quoi l'utilisateur a droit,
/// comme le serveur MCP.
/// </summary>
public class GetAbonnementCourant
{
    /// <summary>
    /// Le forfait courant. Vaut <see cref="ForfaitsAbonnement.Gratuit" /> lorsque
    /// l'utilisateur ne possède aucun abonnement actif.
    /// </summary>
    public string Plan { get; set; } = ForfaitsAbonnement.Gratuit;

    /// <summary>
    /// Vrai lorsqu'un abonnement actif est à l'origine du forfait, faux lorsque le
    /// forfait gratuit est retourné par défaut. Permet de distinguer « abonné au
    /// forfait gratuit » de « aucun abonnement ».
    /// </summary>
    public bool AbonnementActif { get; set; }

    /// <summary>
    /// La date de début de l'abonnement actif, si applicable.
    /// </summary>
    public DateTimeOffset? DateDebut { get; set; }

    /// <summary>
    /// La date de fin de l'abonnement actif, ou null lorsqu'il n'a pas d'échéance.
    /// </summary>
    public DateTimeOffset? DateFin { get; set; }

    /// <summary>
    /// La fréquence de facturation de l'abonnement actif. Voir
    /// <see cref="FrequencesFacturation" />. Null pour un forfait gratuit.
    /// </summary>
    public string? FrequenceFacturation { get; set; }
}
