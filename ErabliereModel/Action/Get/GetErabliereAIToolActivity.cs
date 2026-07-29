using System.Collections.Generic;

namespace ErabliereApi.Donnees.Action.Get;

/// <summary>
/// L'avancement d'un prompt pendant que la réponse se construit.
///
/// L'interface interroge cette ressource pendant qu'elle attend la réponse, pour
/// afficher « Consultation des données de capteurs… » plutôt qu'un sablier muet.
/// </summary>
public class GetErabliereAIToolActivity
{
    /// <summary>
    /// Les étapes publiées jusqu'ici, de la plus ancienne à la plus récente.
    /// </summary>
    public IReadOnlyList<GetErabliereAIToolActivityStep> Steps { get; set; } = [];

    /// <summary>
    /// Vrai lorsque le prompt a fini d'être traité. L'interface cesse alors
    /// d'interroger cette ressource.
    /// </summary>
    public bool Completed { get; set; }
}

/// <summary>
/// Une étape de la boucle d'outils.
/// </summary>
public class GetErabliereAIToolActivityStep
{
    /// <summary>
    /// Le tour de boucle, à partir de 1.
    /// </summary>
    public int Round { get; set; }

    /// <summary>
    /// Le nom de l'outil appelé, nul pendant que le modèle réfléchit.
    /// </summary>
    public string? ToolName { get; set; }

    /// <summary>
    /// La phrase à afficher, déjà en français.
    /// </summary>
    public string Label { get; set; } = "";
}
