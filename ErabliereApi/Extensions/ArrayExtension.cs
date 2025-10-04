namespace ErabliereApi.Extensions;

/// <summary>
/// Extensions pour les tableaux.
/// </summary>
public static class ArrayExtension
{
    /// <summary>
    /// Récupère l'élément à l'index spécifié ou une valeur par défaut si l'index est hors limites.
    /// </summary>
    /// <typeparam name="T">Type des éléments dans le tableau.</typeparam>
    /// <param name="array">Le tableau source.</param>
    /// <param name="index">L'index de l'élément à récupérer.</param>
    /// <param name="defaultValue">La valeur par défaut à retourner si l'index est hors limites. Par défaut, c'est la valeur par défaut de T.</param>
    /// <returns>L'élément à l'index spécifié ou la valeur par défaut de T si l'index est hors limites.</returns>
    public static T AtIndexOrDefault<T>(this T[] array, uint index, T defaultValue = default!)
    {
        if (index >= array.Length)
        {
            return defaultValue;
        }

        return array[index];
    }
}