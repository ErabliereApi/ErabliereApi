using ModelContextProtocol;

namespace ErabliereApi.Mcp.Tools;

/// <summary>
/// Validation helpers shared by the MCP tools. Arguments come from a language
/// model, so they are validated defensively and rejected with an actionable
/// message instead of being forwarded to the API.
/// </summary>
public static class ToolArguments
{
    /// <summary>
    /// Default number of items returned when the caller does not specify one.
    /// </summary>
    public const int DefaultTop = 25;

    /// <summary>
    /// Hard upper bound on the number of items returned by a single tool call,
    /// to keep the result small enough for a model context.
    /// </summary>
    public const int MaxTop = 100;

    /// <summary>
    /// Parses an identifier supplied by the model.
    /// </summary>
    /// <exception cref="McpException">The value is missing or is not a valid GUID.</exception>
    public static Guid ParseId(string? value, string argumentName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new McpException($"The argument '{argumentName}' is required.");
        }

        if (!Guid.TryParse(value.Trim(), out var id))
        {
            throw new McpException($"The argument '{argumentName}' must be a GUID, for example 3fa85f64-5717-4562-b3fc-2c963f66afa6. Received: '{value}'.");
        }

        if (id == Guid.Empty)
        {
            throw new McpException($"The argument '{argumentName}' must not be an empty GUID.");
        }

        return id;
    }

    /// <summary>
    /// Validates the number of items requested by the model.
    /// </summary>
    /// <exception cref="McpException">The value is outside of the allowed range.</exception>
    public static int ValidateTop(int? top)
    {
        if (top is null)
        {
            return DefaultTop;
        }

        if (top < 1 || top > MaxTop)
        {
            throw new McpException($"The argument 'top' must be between 1 and {MaxTop}. Received: {top}.");
        }

        return top.Value;
    }

    /// <summary>
    /// Builds an OData 'contains' filter on the given property, escaping the
    /// single quotes so a search term cannot break out of the string literal.
    /// Returns null when no search term was provided.
    /// </summary>
    public static string? BuildContainsFilter(string propertyName, string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return null;
        }

        var escaped = searchTerm.Trim().Replace("'", "''", StringComparison.Ordinal);

        return $"contains({propertyName},'{escaped}')";
    }
}
