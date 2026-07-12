using System;
using System.Linq;

namespace ErabliereApi.Donnees.Contantes;

/// <summary>
/// Les types de ligne de tubelure supportés
/// </summary>
public static class TypeLigneTubelure
{
    /// <summary>
    /// Ligne principale (maîtresse / main)
    /// </summary>
    public const string Principale = "principale";

    /// <summary>
    /// Ligne secondaire (collectrice)
    /// </summary>
    public const string Secondaire = "secondaire";

    /// <summary>
    /// Ligne latérale (5/16)
    /// </summary>
    public const string Laterale = "laterale";

    /// <summary>
    /// La liste de tous les types de ligne valides
    /// </summary>
    public static readonly string[] Tous = [Principale, Secondaire, Laterale];

    /// <summary>
    /// Valide qu'un type de ligne fait partie de la liste des types supportés
    /// </summary>
    public static bool EstValide(string? type)
    {
        return type != null && Tous.Contains(type.ToLowerInvariant());
    }
}
