using System.ComponentModel;
using System.Reflection;
using ErabliereApi.Mcp.Models;
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

        names.ShouldBe([
            "get_alertes",
            "get_barils",
            "get_dompeux",
            "get_donnees_capteur",
            "get_erabliere",
            "get_horaire",
            "get_notes",
            "get_rapport",
            "list_capteurs",
            "list_erablieres",
            "list_rapports"
        ]);
    }

    [Fact]
    public void TheToolSetStaysCurated()
    {
        // A tool set that grows to one entry per controller stops being usable: the
        // model spends its context reading tool definitions and picks the wrong one.
        // Adding a tool beyond this bound is a design decision, not a detail.
        ToolMethods().Count().ShouldBeLessThanOrEqualTo(12);
    }

    [Fact]
    public void EveryToolReturnsTheStandardEnvelope()
    {
        foreach (var method in ToolMethods())
        {
            var name = method.GetCustomAttribute<McpServerToolAttribute>()!.Name;

            // Task<ToolResponse<T>> is the shared contract, so a model that learned
            // to read {summary, data, truncated} on one tool can read them all.
            var returnType = method.ReturnType;

            returnType.IsGenericType.ShouldBeTrue($"The tool {name} must return a Task.");
            returnType.GetGenericTypeDefinition().ShouldBe(typeof(Task<>), $"The tool {name} must return a Task.");

            var payload = returnType.GetGenericArguments()[0];

            payload.IsGenericType.ShouldBeTrue($"The tool {name} must return a ToolResponse<T>.");
            payload.GetGenericTypeDefinition().ShouldBe(typeof(ToolResponse<>), $"The tool {name} must return a ToolResponse<T>, not a bare payload.");
        }
    }

    [Fact]
    public void EveryToolDescriptionSaysWhatItDoesAndWhenToUseIt()
    {
        foreach (var method in ToolMethods())
        {
            var name = method.GetCustomAttribute<McpServerToolAttribute>()!.Name;
            var description = method.GetCustomAttribute<DescriptionAttribute>()!.Description;

            // The convention of this server: one sentence on what the tool returns,
            // one telling the model when to reach for it.
            var saysWhenToUseIt = description.Contains("Use this", StringComparison.Ordinal) ||
                                  description.Contains("Call this", StringComparison.Ordinal);

            saysWhenToUseIt.ShouldBeTrue(
                $"The description of {name} must tell the model when to use the tool, not only what it does.");

            // Both sentences plus the parameter guidance never fit in less.
            description.Length.ShouldBeGreaterThan(150,
                $"The description of {name} is too short to cover what it does and when to use it.");
        }
    }

    [Fact]
    public void EveryToolTakingADateDocumentsTheExpectedFormat()
    {
        var dateParameters = ToolMethods()
            .SelectMany(method => method.GetParameters(), (method, parameter) => (method, parameter))
            .Where(pair => pair.parameter.Name is not null &&
                           pair.parameter.Name.EndsWith("Date", StringComparison.Ordinal));

        foreach (var (method, parameter) in dateParameters)
        {
            var description = parameter.GetCustomAttribute<DescriptionAttribute>()!.Description;

            description.ShouldContain("ISO 8601", Case.Sensitive,
                $"The parameter {parameter.Name} of {method.Name} must state the date format the model has to produce.");
        }
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
        // Phases 1 and 2 only expose read-only tools, an MCP client may use the
        // hint to skip the confirmation prompt.
        foreach (var method in ToolMethods())
        {
            var attribute = method.GetCustomAttribute<McpServerToolAttribute>()!;

            attribute.ReadOnly.ShouldBe(true, $"The tool {attribute.Name} must be marked as read-only.");
        }
    }
}
