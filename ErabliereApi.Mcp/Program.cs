using ErabliereApi.Mcp.Configuration;
using ErabliereApi.Mcp.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

// The stdio transport writes the JSON-RPC frames on stdout, so every log entry
// must be redirected to stderr, otherwise the MCP client fails to parse the stream.
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services.AddErabliereApiProxy(builder.Configuration);

builder.Services.AddMcpServer()
                .WithStdioServerTransport()
                .WithToolsFromAssembly();

var host = builder.Build();

// Fail fast with a readable message instead of an options validation stack trace:
// the MCP client only shows the process output when the server refuses to start.
var configurationErrors = host.Services
                              .GetRequiredService<IOptions<ErabliereApiMcpOptions>>()
                              .Value
                              .Validate();

if (configurationErrors.Count > 0)
{
    await Console.Error.WriteLineAsync("ErabliereApi.Mcp cannot start:");

    foreach (var error in configurationErrors)
    {
        await Console.Error.WriteLineAsync($"  - {error}");
    }

    return 1;
}

await host.RunAsync();

return 0;
