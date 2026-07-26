using System.Text.Json;
using System.Text.Json.Serialization;

namespace ErabliereApi.Mcp.Serialization;

/// <summary>
/// The single serializer used to turn a tool result into the JSON the model
/// reads.
/// </summary>
/// <remarks>
/// These options are handed to <c>WithToolsFromAssembly</c> in <c>Program.cs</c>,
/// so the payload measured by <see cref="Models.ToolResponse"/> when it enforces
/// the response budget is byte for byte the payload the MCP SDK writes. Sharing
/// one instance is what makes the budget a guarantee instead of an estimate.
/// </remarks>
public static class ToolJson
{
    /// <summary>
    /// Compact, camel cased, ISO 8601 to the second.
    /// </summary>
    public static JsonSerializerOptions Options { get; } = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = false,
            // Null members are kept: a model reading "q": null learns the grade of
            // the barrel is unknown, whereas an absent member is ambiguous with a
            // field this server does not expose.
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            // The payload is JSON-RPC framed, never embedded in HTML, and the data
            // is French: escaping the accented characters would triple their cost.
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            // Reflection based: the projections are plain records and this server
            // is not published ahead of time compiled.
            TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
        };

        options.Converters.Add(new Iso8601SecondsConverter());

        // Frozen once built: the same instance is handed to the MCP SDK and used
        // to measure the response budget, so nothing may reconfigure it later.
        options.MakeReadOnly();

        return options;
    }

    /// <summary>
    /// Serializes a value the way the MCP SDK will.
    /// </summary>
    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);
}
