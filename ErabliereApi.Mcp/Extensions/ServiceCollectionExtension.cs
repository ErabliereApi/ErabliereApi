using ErabliereAPI.Proxy;
using ErabliereApi.Mcp.Configuration;
using ErabliereApi.Mcp.Http;
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
    public static IServiceCollection AddErabliereApiProxy(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ErabliereApiMcpOptions>()
                .Configure(options =>
                {
                    options.BaseUrl = configuration[ErabliereApiMcpOptions.BaseUrlEnvironmentVariable] ?? "";
                    options.ApiKey = configuration[ErabliereApiMcpOptions.ApiKeyEnvironmentVariable] ?? "";
                });
        // No ValidateOnStart here: Program.cs reports the configuration errors
        // itself, because an OptionsValidationException stack trace is unreadable
        // in the log panel of an MCP client.

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

        services.AddSingleton<IErabliereAPIProxy>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ErabliereApiMcpOptions>>().Value;
            var httpClient = sp.GetRequiredService<IHttpClientFactory>()
                               .CreateClient(ErabliereApiMcpOptions.HttpClientName);

            return new ErabliereAPIProxy(options.BaseUrl, httpClient);
        });

        return services;
    }
}
