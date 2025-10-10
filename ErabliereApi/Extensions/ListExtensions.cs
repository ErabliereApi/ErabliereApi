namespace ErabliereApi.Extensions;

/// <summary>
/// Méthodes d'extensions pour les listes
/// </summary>
public static class ListExtensions
{
    /// <summary>
    /// Retrieves the element at the specified index in the list, or the default value for the type if the index is out
    /// of range.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="list">The list from which to retrieve the element. Cannot be <see langword="null"/>.</param>
    /// <param name="index">The zero-based index of the element to retrieve.</param>
    /// <returns>The element at the specified index if it exists; otherwise, the default value for the type <typeparamref
    /// name="T"/>.</returns>
    public static T AtIndexOrDefault<T>(this List<T> list, int index)
    {
        if (index < 0 || index >= list.Count)
        {
            return default!;
        }
        return list[index];
    }
}
