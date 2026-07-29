using System.Collections.Generic;

namespace ErabliereApi.Donnees.Action.Get;

/// <summary>
/// Ce qu'ErabliereAI peut faire pour l'utilisateur authentifié.
///
/// L'interface s'en sert pour savoir s'il faut annoncer que l'assistant consulte
/// les données réelles, ou afficher une invitation discrète à s'abonner.
/// </summary>
public class GetErabliereAICapabilities
{
    /// <summary>
    /// Vrai lorsque l'assistant peut appeler les outils de consultation en lecture
    /// seule. Faux, la conversation fonctionne comme avant : le modèle répond de ses
    /// propres connaissances.
    /// </summary>
    public bool ToolsEnabled { get; set; }

    /// <summary>
    /// Le forfait courant de l'utilisateur.
    /// </summary>
    public string Plan { get; set; } = ForfaitsAbonnement.Gratuit;

    /// <summary>
    /// Vrai lorsque le déploiement restreint les outils selon le forfait. Faux,
    /// <see cref="ToolsEnabled" /> ne dépend pas de l'abonnement et aucune invitation
    /// à s'abonner ne doit être affichée.
    /// </summary>
    public bool PlanGateEnabled { get; set; }

    /// <summary>
    /// Les forfaits qui donnent accès aux outils, pour dire à l'utilisateur à quoi
    /// s'abonner plutôt que seulement ce qui lui manque.
    /// </summary>
    public IReadOnlyList<string> PlansGrantingAccess { get; set; } = [];

    /// <summary>
    /// L'adresse où s'abonner, lorsque le déploiement en a configuré une.
    /// </summary>
    public string? SubscriptionUrl { get; set; }
}
