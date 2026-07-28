using ErabliereApi.Mcp.Serialization;
using ErabliereApi.Mcp.Tools;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using OpenAI.Chat;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;

namespace ErabliereApi.Services.AI.Tools;

/// <summary>
/// Reads the MCP tool set once and keeps the resulting functions and chat tool
/// definitions for the lifetime of the process.
/// </summary>
/// <remarks>
/// Registered as a singleton: reflecting over the assembly and generating a json
/// schema per tool is not work to redo on every prompt, and the result is
/// immutable. What is <em>not</em> cached is the data a tool reads, which is
/// fetched per call with the credentials of the caller.
/// </remarks>
public class ErabliereAiToolCatalog
{
    private readonly Dictionary<string, AIFunction> _functions;
    private readonly IReadOnlyList<ChatTool> _chatTools;

    /// <summary>
    /// Builds the catalog from the tool set of the MCP assembly.
    /// </summary>
    public ErabliereAiToolCatalog(IOptions<ErabliereAiToolOptions> options)
    {
        _functions = BuildFunctions(options.Value);
        _chatTools = [.. _functions.Values.Select(ToChatTool)];
    }

    /// <summary>
    /// The tool definitions handed to the model.
    /// </summary>
    public IReadOnlyList<ChatTool> ChatTools => _chatTools;

    /// <summary>
    /// The names of the exposed tools, in declaration order.
    /// </summary>
    public IReadOnlyCollection<string> ToolNames => _functions.Keys;

    /// <summary>
    /// Finds an exposed tool by the name the model used, or null when the model
    /// invented one.
    /// </summary>
    public AIFunction? Find(string toolName)
    {
        return _functions.GetValueOrDefault(toolName);
    }

    private static Dictionary<string, AIFunction> BuildFunctions(ErabliereAiToolOptions options)
    {
        var functions = new Dictionary<string, AIFunction>(StringComparer.Ordinal);

        foreach (var method in ToolMethods())
        {
            var attribute = method.GetCustomAttribute<McpServerToolAttribute>()!;
            var name = attribute.Name;

            if (string.IsNullOrWhiteSpace(name) || !options.IsExposed(name))
            {
                continue;
            }

            // Read-only is a hard gate, not a hint: phase 8 exposes data to a chat,
            // and a tool that could change something must never reach it, whatever
            // the configuration says.
            if (attribute.ReadOnly != true)
            {
                continue;
            }

            functions[name] = AIFunctionFactory.Create(method, target: null, new AIFunctionFactoryOptions
            {
                Name = name,
                Description = method.GetCustomAttribute<DescriptionAttribute>()?.Description,
                // The very serializer the MCP server hands to the SDK, so what the
                // model reads here is byte for byte what an MCP client would read,
                // response budget included.
                SerializerOptions = ToolJson.Options,
                ConfigureParameterBinding = BindServiceParameters
            });
        }

        return functions;
    }

    /// <summary>
    /// The convention of the MCP tool set: an interface parameter is supplied by the
    /// host, everything else is filled by the model. It keeps <c>IErabliereAPIProxy</c>
    /// and the like out of the schema, so the model never sees — let alone chooses —
    /// what a tool reads the data with.
    /// </summary>
    private static AIFunctionFactoryOptions.ParameterBindingOptions BindServiceParameters(ParameterInfo parameter)
    {
        if (!parameter.ParameterType.IsInterface)
        {
            return default;
        }

        return new AIFunctionFactoryOptions.ParameterBindingOptions
        {
            ExcludeFromSchema = true,
            BindParameter = (p, arguments) =>
                arguments.Services?.GetService(p.ParameterType)
                ?? throw new InvalidOperationException(
                    $"The tool parameter '{p.Name}' of type {p.ParameterType.Name} could not be resolved from the request services.")
        };
    }

    private static IEnumerable<MethodInfo> ToolMethods()
    {
        return typeof(ErabliereTools).Assembly
            .GetTypes()
            .Where(type => type.GetCustomAttribute<McpServerToolTypeAttribute>() is not null)
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(method => method.GetCustomAttribute<McpServerToolAttribute>() is not null);
    }

    private static ChatTool ToChatTool(AIFunction function)
    {
        return ChatTool.CreateFunctionTool(
            function.Name,
            function.Description,
            BinaryData.FromString(WithoutSchemaKeyword(function.JsonSchema)));
    }

    /// <summary>
    /// Drops the '$schema' keyword from the generated schema. It is legal json
    /// schema, but the chat completion apis validate the function parameters against
    /// a narrower dialect and reject unknown root keywords.
    /// </summary>
    private static string WithoutSchemaKeyword(JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object)
        {
            return schema.GetRawText();
        }

        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();

            foreach (var property in schema.EnumerateObject())
            {
                if (property.NameEquals("$schema"))
                {
                    continue;
                }

                property.WriteTo(writer);
            }

            writer.WriteEndObject();
        }

        return System.Text.Encoding.UTF8.GetString(buffer.ToArray());
    }
}
