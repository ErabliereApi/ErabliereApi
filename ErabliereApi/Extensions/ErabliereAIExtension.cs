using ErabliereAPI.Proxy;
using ErabliereApi.Mcp.Configuration;
using ErabliereApi.Services.AI;
using ErabliereApi.Services.AI.Tools;
using Microsoft.Extensions.Options;

namespace ErabliereApi.Extensions;

/// <summary>
/// Registration of ErabliereAI: the provider, the prompt builder, and everything
/// the read-only tool set needs to run in process.
/// </summary>
public static class ErabliereAIExtension
{
    /// <summary>
    /// Registers ErabliereAI and its tools.
    /// </summary>
    public static IServiceCollection AddErabliereAI(this IServiceCollection services, IConfiguration configuration)
    {
        if (string.Equals(configuration["PrimaryAIService"]?.Trim(), "Google", StringComparison.OrdinalIgnoreCase))
        {
            services.AddTransient<IAIService, GeminiAIService>();
        }
        else
        {
            services.AddTransient<IAIService, AzureOpenAIService>();
        }

        services.AddTransient<ISystemPromptBuilder, SystemPromptBuilder>();
        services.AddTransient<IConversationAIService, ConversationAIService>();

        return services.AddErabliereAITools(configuration);
    }

    /// <summary>
    /// Registers the tool set the chat calls to answer with real data.
    /// </summary>
    /// <remarks>
    /// The tools themselves come from the MCP assembly, unchanged. What is
    /// registered here is how they reach ErabliereAPI: a named http client whose
    /// only message handler forwards the credentials of the request being served.
    /// The AI is therefore never given a credential of its own, and cannot read one
    /// row more than the user who asked the question.
    /// </remarks>
    public static IServiceCollection AddErabliereAITools(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ErabliereAiToolOptions>()
                .Bind(configuration.GetSection(ErabliereAiToolOptions.SectionName));

        // The same section the MCP server reads (phase 4): a deployment decides once
        // which plans may point an AI at their data.
        services.AddOptions<McpPlanGatingOptions>()
                .Bind(configuration.GetSection(McpPlanGatingOptions.SectionName));

        services.AddMemoryCache();
        services.AddHttpContextAccessor();

        services.AddSingleton<ErabliereAiToolCatalog>();
        services.AddSingleton<LoopbackRequestMarker>();
        services.AddSingleton<IToolActivityTracker, MemoryCacheToolActivityTracker>();
        services.AddScoped<IErabliereAiCapabilityService, ErabliereAiCapabilityService>();
        services.AddScoped<IErabliereAiToolset, McpErabliereAiToolset>();

        services.AddTransient<CallerCredentialsHandler>();

        services.AddHttpClient(ErabliereAiToolOptions.HttpClientName)
                .AddHttpMessageHandler<CallerCredentialsHandler>();

        // Scoped, like the proxy of the MCP server in HTTP mode: one instance per
        // request, so an instance is never shared between two callers.
        services.AddScoped<IErabliereAPIProxy>(CreateToolProxy);

        return services;
    }

    private static IErabliereAPIProxy CreateToolProxy(IServiceProvider serviceProvider)
    {
        var options = serviceProvider.GetRequiredService<IOptions<ErabliereAiToolOptions>>().Value;

        var httpClient = serviceProvider.GetRequiredService<IHttpClientFactory>()
                                        .CreateClient(ErabliereAiToolOptions.HttpClientName);

        var baseUrl = ResolveBaseUrl(options, serviceProvider.GetRequiredService<IHttpContextAccessor>());

        return new ErabliereAPIProxy(baseUrl, httpClient);
    }

    /// <summary>
    /// Where the tools call ErabliereAPI back. The address of the request being
    /// served is the right answer for a single deployment and needs no configuration;
    /// <c>ErabliereAI:Tools:ApiBaseUrl</c> overrides it when the API is reachable
    /// internally on another address than the one the browser used.
    /// </summary>
    private static string ResolveBaseUrl(ErabliereAiToolOptions options, IHttpContextAccessor httpContextAccessor)
    {
        if (!string.IsNullOrWhiteSpace(options.ApiBaseUrl))
        {
            return options.ApiBaseUrl.TrimEnd('/');
        }

        var request = httpContextAccessor.HttpContext?.Request
            ?? throw new InvalidOperationException(
                $"The ErabliereAI tools need a base url for ErabliereAPI. Outside of an http request, set {ErabliereAiToolOptions.SectionName}:ApiBaseUrl.");

        return $"{request.Scheme}://{request.Host}";
    }
}
