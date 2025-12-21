namespace ErabliereApi.Extensions;

/// <summary>
/// Extension method for any object
/// </summary>
public static class ObjectExtension
{
    /// <summary>
    /// Map an object instance into another
    /// </summary>
    /// <typeparam name="TDestination"></typeparam>
    /// <param name="source"></param>
    /// <returns></returns>
    public static TDestination MapTo<TDestination>(this object source)
    {
        var dest = Activator.CreateInstance<TDestination>();

        var ps = source.GetType().GetProperties();
        var pd = typeof(TDestination).GetProperties().ToDictionary(p => p.Name);

        foreach (var p in ps)
        {
            if (pd.TryGetValue(p.Name, out var pDest))
            {
                var value = p.GetValue(source);
                pDest.SetValue(dest, value);
            }
        }

        return dest;
    }

    /// <summary>
    /// Map an object instance into another with a dictionary for specific mapping function
    /// </summary>
    /// <typeparam name="TDestination"></typeparam>
    /// <param name="source"></param>
    /// <param name="specificMapping"></param>
    /// <returns></returns>
    public static TDestination MapTo<TDestination>(
        this object source, Dictionary<string, Func<object?, object?>> specificMapping)
    {
        var dest = Activator.CreateInstance<TDestination>();

        var ps = source.GetType().GetProperties();
        var pd = typeof(TDestination).GetProperties().ToDictionary(p => p.Name);

        foreach (var p in ps)
        {
            if (pd.TryGetValue(p.Name, out var pDest))
            {
                if (specificMapping.TryGetValue(p.Name, out var func))
                {
                    var value = func(source);
                    pDest.SetValue(dest, value);
                }
                else
                {
                    var value = p.GetValue(source);
                    pDest.SetValue(dest, value);
                }
            }
        }

        return dest;
    }
}
