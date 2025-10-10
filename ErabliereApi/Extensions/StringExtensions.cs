using Org.BouncyCastle.Utilities.Encoders;

namespace ErabliereApi.Extensions;

/// <summary>
/// String Extensions
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Try parse a base64 string, it return true if its valide
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool IsValidBase64(this string value)
    {
        try
        {
            Base64.Decode(value);

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Try parse a base64 string, it return true if its valide
    /// </summary>
    /// <param name="value"></param>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public static bool IsValidBase64(this string value, out byte[] bytes)
    {
        try
        {
            bytes = Base64.Decode(value);

            return true;
        }
        catch
        {
            bytes = Array.Empty<byte>();

            return false;
        }
    }

    /// <summary>
    /// Sanatize a string by removing \n and \r
    /// </summary>
    public static string? Sanatize(this string? value)
    {
        return value?.Replace("\n", "")?.Replace("\r", "");
    }

    /// <summary>
    /// Split a CSV line into multiple lines based on commas, ignoring commas inside quotes
    /// </summary>
    /// <param name="line"></param>
    /// <param name="seperator"></param>
    /// <returns></returns>
    public static List<string> SplitCSVLine(this string? line, char seperator = ',')
    {
        if (line == null)
        {
            return [];
        }
        var list = new List<string>();
        var inQuotes = false;
        var value = new System.Text.StringBuilder();
        for (var i = 0; i < line.Length; i++)
        {
            var currentChar = line[i];
            if (currentChar == '\"')
            {
                inQuotes = !inQuotes;
            }
            else if (currentChar == seperator && !inQuotes)
            {
                list.Add(value.ToString());
                value.Clear();
            }
            else
            {
                value.Append(currentChar);
            }
        }
        return list;
    }
}
