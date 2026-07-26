using ErabliereAPI.Proxy;
using ErabliereApi.Mcp.Configuration;
using ErabliereApi.Mcp.Health;
using ErabliereApi.Mcp.Hosting;
using ErabliereApi.Mcp.Http;
using ErabliereApi.Mcp.Serialization;
using ErabliereApi.Mcp.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ErabliereApi.Mcp.Extensions;

/// <summary>
/// Registration of the ErabliereAPI client used by the MCP tools.
/// </summary>
public static class ServiceCollectionExtension
{
    /// <summary>
    /// Registers <see cref="IErabliereAPIProxy"/> and everything it needs:
    /// options read from the environment, the api key handler and the retry
    /// handler already shipped by the ErabliereAPI.Proxy project.
    /// </summary>
    /// <param name="services">Collection to register into.</param>
    /// <param name="configuration">Configuration holding the environment variables.</param>
    /// <param name="transportMode">
    /// Transport being started. It decides where the api key comes from and how
    /// long a proxy instance lives.
    /// </param>
    public static IServiceCollection AddErabliereApiProxy(
        this IServiceCollection services,
        IConfiguration configuration,
        McpTransportMode transportMode = McpTransportMode.Stdio)
    {
        services.AddOptions<ErabliereApiMcpOptions>()
                .Configure(options =>
                {
                    options.BaseUrl = configuration[ErabliereApiMcpOptions.BaseUrlEnvironmentVariable] ?? "";
                    options.ApiKey = configuration[ErabliereApiMcpOptions.ApiKeyEnvironmentVariable] ?? "";
                });
        // No ValidateOnStart here: the transport runners report the configuration
        // errors themselves, because an OptionsValidationException stack trace is
        // unreadable in the log panel of an MCP client.

        if (transportMode == McpTransportMode.Http)
        {
            services.AddHttpContextAccessor();
            services.AddSingleton<IApiKeyAccessor, HttpHeaderApiKeyAccessor>();
        }
        else
        {
            services.AddSingleton<IApiKeyAccessor, ConfiguredApiKeyAccessor>();
        }

        services.AddOptions<McpPlanGatingOptions>()
                .Bind(configuration.GetSection(McpPlanGatingOptions.SectionName));

        // Registered for both transports: the gate is HTTP only, but get_my_plan is
        // offered by both, so the tool set stays identical whatever the transport.
        services.AddMemoryCache();
        services.AddScoped<ISubscriptionPlanResolver, ErabliereApiSubscriptionPlanResolver>();

        services.AddTransient<ApiKeyHandler>();
        services.AddTransient(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ErabliereApiMcpOptions>>().Value;

            return new RetryPolicyHandler(options.MaxRetries, options.RetryDelay);
        });

        services.AddHttpClient(ErabliereApiMcpOptions.HttpClientName)
                // The retry handler is the outermost one so the api key is
                // re-applied on every attempt.
                .AddHttpMessageHandler<RetryPolicyHandler>()
                .AddHttpMessageHandler<ApiKeyHandler>();

        if (transportMode == McpTransportMode.Http)
        {
            // Scoped, not singleton: the hosted server answers several callers and
            // the MCP SDK opens a scope per request (McpServerOptions.ScopeRequests),
            // so no proxy instance is ever shared between two api keys.
            services.AddScoped<IErabliereAPIProxy>(CreateProxy);
        }
        else
        {
            services.AddSingleton<IErabliereAPIProxy>(CreateProxy);
        }

        return services;
    }

    /// <summary>
    /// Registers the MCP server itself with the tool set of this assembly. Shared
    /// by both transports, which only differ by the transport they chain onto it.
    /// </summary>
    public static IMcpServerBuilder AddErabliereApiMcpServer(this IServiceCollection services)
    {
        // Passing the shared options is what makes the response budget a
        // guarantee: the payload ToolResponse measures is byte for byte the
        // payload written on the wire.
        return services.AddMcpServer()
                       .WithToolsFromAssembly(typeof(ToolJson).Assembly, ToolJson.Options);
    }

    /// <summary>
    /// Registers the liveness and readiness checks of the HTTP transport.
    /// </summary>
    public static IServiceCollection AddErabliereApiMcpHealthChecks(this IServiceCollection services)
    {
        // No api key handler and no retry handler on this client: a probe reports
        // the state of the moment.
        services.AddHttpClient(ErabliereApiHealthCheck.HttpClientName)
                .ConfigureHttpClient(client => client.Timeout = TimeSpan.FromSeconds(5));

        services.AddHealthChecks()
                .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("The MCP server process is running."), tags: ["live"])
                .AddCheck<ErabliereApiHealthCheck>("erabliereapi", tags: ["ready"]);

        return services;
    }

    private static IErabliereAPIProxy CreateProxy(IServiceProvider sp)
    {
        var options = sp.GetRequiredService<IOptions<ErabliereApiMcpOptions>>().Value;
        var httpClient = sp.GetRequiredService<IHttpClientFactory>()
                           .CreateClient(ErabliereApiMcpOptions.HttpClientName);

        return new ErabliereAPIProxy(options.BaseUrl, httpClient);
    }
}
