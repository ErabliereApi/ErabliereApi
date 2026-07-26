using ErabliereApi.Mcp.Configuration;
using ErabliereApi.Mcp.Extensions;
using ErabliereApi.Mcp.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ErabliereApi.Mcp.Hosting;

/// <summary>
/// Runs the MCP server over the Streamable HTTP transport, the transport used
/// when the server is hosted and reached remotely by its MCP clients.
/// </summary>
/// <remarks>
/// The composition is split in <see cref="ConfigureServices"/> and
/// <see cref="MapEndpoints"/> so the integration test boots the very same
/// pipeline on a test server instead of a copy of it.
/// </remarks>
public static class HttpServerRunner
{
    /// <summary>
    /// Path the MCP endpoint is mapped to.
    /// </summary>
    public const string McpPath = "/mcp";

    /// <summary>
    /// Path of the readiness probe: the process is up and ErabliereAPI answers.
    /// </summary>
    public const string HealthPath = "/health";

    /// <summary>
    /// Path of the liveness probe: the process is up. Same route names as
    /// ErabliereAPI itself, so both deployments are probed the same way.
    /// </summary>
    public const string LivePath = "/live";

    /// <summary>
    /// Builds and runs the server. Returns the process exit code.
    /// </summary>
    public static async Task<int> RunAsync(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        ConfigureServices(builder);

        var app = builder.Build();

        // Same fail fast as the stdio transport: a container that answers 500 on
        // every call is harder to diagnose than one that refuses to start.
        var configurationErrors = app.Services
                                     .GetRequiredService<IOptions<ErabliereApiMcpOptions>>()
                                     .Value
                                     .Validate(McpTransportMode.Http)
                                     .Concat(app.Services
                                                .GetRequiredService<IOptions<McpPlanGatingOptions>>()
                                                .Value
                                                .Validate())
                                     .ToArray();

        if (configurationErrors.Length > 0)
        {
            await StdioServerRunner.WriteConfigurationErrorsAsync(configurationErrors);

            return 1;
        }

        MapEndpoints(app);

        await app.RunAsync();

        return 0;
    }

    /// <summary>
    /// Registers everything the HTTP transport needs.
    /// </summary>
    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddErabliereApiProxy(builder.Configuration, McpTransportMode.Http);
        builder.Services.AddErabliereApiMcpHealthChecks();

        builder.Services.AddErabliereApiMcpServer()
                        .WithHttpTransport(options =>
                        {
                            // Stateless: no session state to keep, so any instance
                            // can answer any request and no load balancer needs
                            // session affinity. It also closes the door on a session
                            // opened with one api key being reused with another,
                            // since every request carries its own key.
                            options.Stateless = true;

                            // Left at its default of false on purpose: a tool
                            // handler then runs with the HttpContext of the request
                            // that carried the tools/call, which is what
                            // HttpHeaderApiKeyAccessor reads the api key from.
                            options.PerSessionExecutionContext = false;
                        });
    }

    /// <summary>
    /// Maps the MCP endpoint and the probes.
    /// </summary>
    public static void MapEndpoints(WebApplication app)
    {
        // Gate the whole MCP endpoint rather than the tools only: initialize and
        // tools/list must be authenticated too. The probes stay anonymous so an
        // orchestrator can reach them without holding a key.
        app.UseWhen(
            context => context.Request.Path.StartsWithSegments(McpPath),
            branch =>
            {
                branch.UseMiddleware<RequireApiKeyMiddleware>();

                // Order matters: the plan is read with the caller's api key, so the
                // key has to be known to be there first.
                branch.UseMiddleware<RequirePlanMiddleware>();
            });

        app.MapMcp(McpPath);

        app.MapHealthChecks(HealthPath, new HealthCheckOptions
        {
            Predicate = registration => !registration.Tags.Contains("live")
        });

        app.MapHealthChecks(LivePath, new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("live")
        });
    }
}
