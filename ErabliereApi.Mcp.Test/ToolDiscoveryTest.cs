using System.ComponentModel;
using System.Reflection;
using ErabliereApi.Mcp.Tools;
using ModelContextProtocol.Server;
using Shouldly;

namespace ErabliereApi.Mcp.Test;

/// <summary>
/// WithToolsFromAssembly() discovers the tools through the attributes, and the
/// tool names and descriptions are the contract seen by the model. This test
/// makes a rename or a missing description a build failure instead of a
/// silently broken MCP server.
/// </summary>
public class ToolDiscoveryTest
{
    private static readonly Assembly ServerAssembly = typeof(ErabliereTools).Assembly;

    private static IEnumerable<MethodInfo> ToolMethods()
    {
        return ServerAssembly.GetTypes()
                             .Where(type => type.GetCustomAttribute<McpServerToolTypeAttribute>() is not null)
                             .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                             .Where(method => method.GetCustomAttribute<McpServerToolAttribute>() is not null);
    }

    [Fact]
    public void TheAssemblyExposesTheExpectedTools()
    {
        var names = ToolMethods()
            .Select(method => method.GetCustomAttribute<McpServerToolAttribute>()!.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        names.ShouldBe(["get_alertes", "get_erabliere", "list_erablieres"]);
    }

    [Fact]
    public void EveryToolHasADescription()
    {
        foreach (var method in ToolMethods())
        {
            var description = method.GetCustomAttribute<DescriptionAttribute>();

            description.ShouldNotBeNull($"The tool {method.Name} must have a [Description] telling the model when to use it.");
            description.Description.Length.ShouldBeGreaterThan(40, $"The description of {method.Name} is too short to be useful.");
        }
    }

    [Fact]
    public void EveryToolParameterHasADescription()
    {
        foreach (var method in ToolMethods())
        {
            var parameters = method.GetParameters()
                                   // Injected by the server, never filled by the model.
                                   .Where(parameter => parameter.ParameterType != typeof(CancellationToken) &&
                                                       !parameter.ParameterType.IsInterface);

            foreach (var parameter in parameters)
            {
                parameter.GetCustomAttribute<DescriptionAttribute>()
                         .ShouldNotBeNull($"The parameter {parameter.Name} of {method.Name} must have a [Description].");
            }
        }
    }

    [Fact]
    public void EveryToolIsMarkedReadOnly()
    {
        // Phase 1 only exposes read-only tools, an MCP client may use the hint
        // to skip the confirmation prompt.
        foreach (var method in ToolMethods())
        {
            var attribute = method.GetCustomAttribute<McpServerToolAttribute>()!;

            attribute.ReadOnly.ShouldBe(true, $"The tool {attribute.Name} must be marked as read-only.");
        }
    }
}
