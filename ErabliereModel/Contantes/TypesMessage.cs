using System;

namespace ErabliereApi.Donnees.Contantes;

/// <summary>
/// Les types de message d'une conversation ErabliereAI.
///
/// Une conversation ne contenait que du texte avant l'intégration des outils MCP.
/// Les messages existants portent donc un type nul, qui se lit comme
/// <see cref="Texte" /> : aucune migration de données n'est nécessaire.
/// </summary>
public static class TypesMessage
{
    /// <summary>
    /// Un message écrit par l'utilisateur ou une réponse rédigée par le modèle.
    /// C'est le seul type affiché tel quel dans la conversation.
    /// </summary>
    public const string Texte = "Texte";

    /// <summary>
    /// Un appel d'outil demandé par le modèle. Le contenu est le JSON des arguments.
    /// </summary>
    public const string AppelOutil = "AppelOutil";

    /// <summary>
    /// Le résultat d'un appel d'outil. Le contenu est l'enveloppe JSON
    /// { summary, data, truncated } retournée par l'outil, ou { error } en cas d'échec.
    /// </summary>
    public const string ResultatOutil = "ResultatOutil";

    /// <summary>
    /// Indique si le type reçu est un message d'outil, c'est-à-dire une trace
    /// technique plutôt qu'un tour de parole.
    /// </summary>
    public static bool EstMessageOutil(string? type)
    {
        return string.Equals(type, AppelOutil, StringComparison.Ordinal) ||
               string.Equals(type, ResultatOutil, StringComparison.Ordinal);
    }
}
