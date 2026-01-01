using Microsoft.Graph.Models;

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
    /// Map a type into another with a dictionary for specific name mapping
    /// when the property names are different
    /// </summary>
    public static TDestination MapTo<TSource, TDestination>(this TSource source, Dictionary<string, string> specificNamedMapping)
    {
        var dest = Activator.CreateInstance<TDestination>();
        var ps = typeof(TSource).GetProperties();
        var pd = typeof(TDestination).GetProperties().ToDictionary(p => p.Name);
        foreach (var p in ps)
        {
            string destName = p.Name;
            if (specificNamedMapping.TryGetValue(p.Name, out var mappedName))
            {
                destName = mappedName;
            }
            if (pd.TryGetValue(destName, out var pDest))
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
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TDestination"></typeparam>
    /// <param name="source"></param>
    /// <param name="specificMapping"></param>
    /// <returns></returns>
    public static TDestination MapTo<TSource, TDestination>(
        this TSource source, Dictionary<string, Func<TSource?, object?>> specificMapping)
    {
        var dest = Activator.CreateInstance<TDestination>();

        var ps = typeof(TSource).GetProperties();
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

    /// <summary>
    /// Map an array of object into another array of object
    /// </summary>
    public static TDestination[] ArrayMapTo<TSource, TDestination>(this TSource[] sourceArray)
    {
        var ps = typeof(TSource).GetProperties();
        var pd = typeof(TDestination).GetProperties().ToDictionary(p => p.Name);

        var result = new TDestination[sourceArray.Length];
        for (int i = 0; i < sourceArray.Length; i++)
        {
            var resultInstance = Activator.CreateInstance<TDestination>();
            result[i] = resultInstance;
            var source = sourceArray[i];

            foreach (var p in ps)
            {
                if (pd.TryGetValue(p.Name, out var pDest))
                {
                    var value = p.GetValue(source);
                    pDest.SetValue(resultInstance, value);
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Update the destination object from the source object.
    /// It only updates the properties that exist in both objects.
    /// It also only updates the values when the propoerty of the source is not the default value.
    /// </summary>
    public static void UpdateFrom<TSource, TDestination>(this TDestination destination, TSource source)
    {
        var ps = typeof(TSource).GetProperties();
        var pd = typeof(TDestination).GetProperties().ToDictionary(p => p.Name);

        foreach (var p in pd)
        {
            var propName = p.Key;
            if (ps.Any(s => s.Name == propName))
            {
                var pSource = ps.First(s => s.Name == propName);
                var value = pSource.GetValue(source);

                if (value == null)
                {
                    continue;
                }

                var defaultValue = pSource.PropertyType.IsValueType ? Activator.CreateInstance(pSource.PropertyType) : null;
                if (value.Equals(defaultValue))
                {
                    continue;
                }
                
                p.Value.SetValue(destination, value);
            }
        }
    }
}
