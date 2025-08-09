using OpenAI.Images;

namespace ErabliereApi.Extensions;

/// <summary>
/// Helper extensions for OpenAI SDK.
/// </summary>
public static class OpenAIExtension
{
    /// <summary>
    /// Convert a string in the format "WIDTHxHEIGHT" to a <see cref="GeneratedImageSize"/>.
    /// </summary>
    /// <param name="size"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static GeneratedImageSize ToGeneratedImageSize(this string size)
    {
        var dim = size.Split('x');

        if (dim.Length != 2 || !int.TryParse(dim[0], out var width) || !int.TryParse(dim[1], out var height))
        {
            throw new ArgumentException("Size must be in the format 'WIDTHxHEIGHT', e.g., '1024x1024'.");
        }

        return new GeneratedImageSize(width, height);
    }
}
