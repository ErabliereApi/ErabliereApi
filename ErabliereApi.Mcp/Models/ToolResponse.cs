using System.Text.Json.Serialization;
using ErabliereApi.Mcp.Serialization;

namespace ErabliereApi.Mcp.Models;

/// <summary>
/// The envelope every tool of this server returns.
/// </summary>
/// <typeparam name="T">Shape of the payload, a list for the listing tools.</typeparam>
/// <param name="Summary">
/// One sentence digesting the payload. A model can very often answer the user
/// from this line alone, without walking <paramref name="Data"/>.
/// </param>
/// <param name="Data">The payload itself.</param>
/// <param name="Truncated">
/// True when the answer is incomplete, either because the API capped the query or
/// because the payload had to be trimmed to fit the response budget.
/// </param>
public record ToolResponse<T>(
    [property: JsonPropertyName("summary")] string Summary,
    [property: JsonPropertyName("data")] T Data,
    [property: JsonPropertyName("truncated")] bool Truncated);

/// <summary>
/// Factories building a <see cref="ToolResponse{T}"/> that fits in the model
/// context.
/// </summary>
public static class ToolResponse
{
    /// <summary>
    /// Ceiling on the size of a single tool result, in tokens. A tool result is
    /// only ever one of the many things competing for the context window, so it
    /// stays well under what the transport would allow.
    /// </summary>
    public const int MaxResponseTokens = 4000;

    /// <summary>
    /// Characters per token used to size a response. Four is the usual rule of
    /// thumb for English and French prose; JSON is denser in punctuation, so this
    /// over-estimates the token count, which is the safe direction.
    /// </summary>
    public const int CharactersPerToken = 4;

    /// <summary>
    /// Builds a response for a single object. No trimming happens here: the
    /// callers producing single objects control their own size, and silently
    /// amputating an object would leave the model with a plausible but false view
    /// of it.
    /// </summary>
    public static ToolResponse<T> ForItem<T>(string summary, T data, bool truncated = false)
    {
        return new ToolResponse<T>(summary, data, truncated);
    }

    /// <summary>
    /// Builds a response for a list, dropping items from the tail until the
    /// serialized envelope fits in <see cref="MaxResponseTokens"/>.
    /// </summary>
    /// <param name="summary">The digest of the whole list, before any trimming.</param>
    /// <param name="items">The items, most relevant first: the tail is what gets dropped.</param>
    /// <param name="truncated">True when the API already capped the result upstream.</param>
    public static ToolResponse<IReadOnlyList<T>> ForList<T>(string summary, IReadOnlyList<T> items, bool truncated = false)
    {
        ArgumentNullException.ThrowIfNull(items);

        var kept = items;

        while (kept.Count > 0 && EstimateTokens(new ToolResponse<IReadOnlyList<T>>(summary, kept, truncated)) > MaxResponseTokens)
        {
            // Geometric shrink: a linear walk would serialize the payload hundreds
            // of times on a list that is far too large.
            var next = kept.Count * 3 / 4;

            kept = [.. kept.Take(next < kept.Count ? next : kept.Count - 1)];
        }

        if (kept.Count == items.Count)
        {
            return new ToolResponse<IReadOnlyList<T>>(summary, kept, truncated);
        }

        var trimmedSummary = $"{summary} Only the first {kept.Count} of {items.Count} items are included, the rest did not fit in the response budget; narrow the query to see them.";

        return new ToolResponse<IReadOnlyList<T>>(trimmedSummary, kept, true);
    }

    /// <summary>
    /// Estimates how many tokens a value costs once serialized the way the MCP
    /// SDK serializes it.
    /// </summary>
    public static int EstimateTokens<T>(T value)
    {
        return ToolJson.Serialize(value).Length / CharactersPerToken;
    }
}
