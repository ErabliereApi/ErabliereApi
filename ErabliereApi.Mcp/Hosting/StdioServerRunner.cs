using ErabliereApi.Mcp.Configuration;
using ErabliereApi.Mcp.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ErabliereApi.Mcp.Hosting;

/// <summary>
/// Runs the MCP server over stdio, the transport used when an MCP client starts
/// this executable as a child process.
/// </summary>
public static class StdioServerRunner
{
    /// <summary>
    /// Builds and runs the server. Returns the process exit code.
    /// </summary>
    public static async Task<int> RunAsync(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // The stdio transport writes the JSON-RPC frames on stdout, so every log entry
        // must be redirected to stderr, otherwise the MCP client fails to parse the stream.
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(options =>
        {
            options.LogToStandardErrorThreshold = LogLevel.Trace;
        });

        builder.Services.AddErabliereApiProxy(builder.Configuration, McpTransportMode.Stdio);

        builder.Services.AddErabliereApiMcpServer()
                        .WithStdioServerTransport();

        var host = builder.Build();

        // Fail fast with a readable message instead of an options validation stack trace:
        // the MCP client only shows the process output when the server refuses to start.
        var configurationErrors = host.Services
                                      .GetRequiredService<IOptions<ErabliereApiMcpOptions>>()
                                      .Value
                                      .Validate(McpTransportMode.Stdio);

        if (configurationErrors.Count > 0)
        {
            await WriteConfigurationErrorsAsync(configurationErrors);

            return 1;
        }

        await host.RunAsync();

        return 0;
    }

    /// <summary>
    /// Prints the configuration errors on stderr, one per line.
    /// </summary>
    internal static async Task WriteConfigurationErrorsAsync(IReadOnlyList<string> configurationErrors)
    {
        await Console.Error.WriteLineAsync("ErabliereApi.Mcp cannot start:");

        foreach (var error in configurationErrors)
        {
            await Console.Error.WriteLineAsync($"  - {error}");
        }
    }
}
