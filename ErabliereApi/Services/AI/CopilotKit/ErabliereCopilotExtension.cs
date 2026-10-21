using Azure;
using Azure.AI.OpenAI;
using ErabliereApi.Attributes;
using ErabliereApi.Donnees;
using ErabliereApi.Services.AI.Tools;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace ErabliereApi.Services.AI.CopilotKit;

/// <summary>
/// Proof of concept: ErabliereAI exposed as an AG-UI agent, the protocol the
/// CopilotKit front-end components speak.
/// </summary>
/// <remarks>
/// Lives next to the ErabliereAI chat rather than replacing it, and is off unless
/// <c>ErabliereAI:CopilotKit:Enabled</c> is true. It reuses what the chat already
/// trusts: the system prompt, the read-only tool set run as the caller, the plan
/// gating of the tools and the subscription required by <c>ErabliereAIController</c>.
/// <para>
/// Not carried over: persistence of the conversations (the browser sends the whole
/// history on every run), the Anthropic provider, the token budget of
/// the tool loop, and public conversations.
/// </para>
/// </remarks>
public static class ErabliereCopilotExtension
{
    /// <summary>
    /// Configuration key turning the proof of concept on.
    /// </summary>
    public const string EnabledKey = "ErabliereAI:CopilotKit:Enabled";

    /// <summary>
    /// Route of the AG-UI endpoint.
    /// </summary>
    public const string Route = "/ErabliereAI/Copilot";

    /// <summary>
    /// Name of the agent, and key of the <see cref="IChatClient" /> it talks to. A
    /// test replaces that keyed client to script the model.
    /// </summary>
    public const string AgentName = "erabliere-copilot";

    /// <summary>
    /// Whether the deployment turned the proof of concept on.
    /// </summary>
    public static bool IsEnabled(IConfiguration configuration)
    {
        return string.Equals(configuration[EnabledKey], "true", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Registers the AG-UI agent. Requires <c>AddErabliereAI</c>.
    /// </summary>
    public static IServiceCollection AddErabliereCopilot(this IServiceCollection services, IConfiguration configuration)
    {
        if (!IsEnabled(configuration))
        {
            return services;
        }

        services.AddAGUIServer();

        services.TryAddKeyedSingleton<IChatClient>(AgentName, (_, _) => CreateChatClient(configuration));

        services.AddSingleton<AIAgent>(CreateAgent);

        return services;
    }

    /// <summary>
    /// Maps the AG-UI endpoint, behind the same authorization as <c>ErabliereAIController</c>.
    /// </summary>
    public static IEndpointRouteBuilder MapErabliereCopilot(this IEndpointRouteBuilder endpoints, IConfiguration configuration)
    {
        if (!IsEnabled(configuration))
        {
            return endpoints;
        }

        var agent = endpoints.ServiceProvider.GetRequiredService<AIAgent>();

        endpoints.MapAGUIServer(Route, agent)
                 .RequireAuthorization(new AuthorizeAttribute { Roles = "ErabliereAIUser", Policy = "TenantIdPrincipal" })
                 .AddEndpointFilter(async (context, next) =>
                 {
                     if (!await ValiderAbonnementAttribute.EstPermisAsync(context.HttpContext, [ForfaitsAbonnement.Base]))
                     {
                         return Results.Forbid();
                     }

                     return await next(context);
                 });

        return endpoints;
    }

    /// <summary>
    /// The model, chosen by <c>PrimaryAIService</c> like the ErabliereAI chat and
    /// configured with the same keys as <c>GeminiAIService</c> and
    /// <c>AzureOpenAIService</c>. Anthropic is not part of the proof of concept.
    /// </summary>
    private static IChatClient CreateChatClient(IConfiguration configuration)
    {
        var primaryAIService = configuration["PrimaryAIService"]?.Trim();

        if (string.Equals(primaryAIService, "Google", StringComparison.OrdinalIgnoreCase))
        {
            return new Google.GenAI.Client(apiKey: configuration["GoogleGenAIKey"])
                .AsIChatClient(configuration["GoogleGenAIModel"] ?? "gemini-2.5-flash");
        }

        if (string.Equals(primaryAIService, "Anthropic", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"{EnabledKey} does not support PrimaryAIService=Anthropic yet.");
        }

        return new AzureOpenAIClient(
                new Uri(configuration["AzureOpenAIUri"] ?? throw new InvalidOperationException($"{EnabledKey} requires AzureOpenAIUri.")),
                new AzureKeyCredential(configuration["AzureOpenAIKey"] ?? ""))
            .GetChatClient(configuration["AzureOpenAIDeploymentChatModelName"])
            .AsIChatClient();
    }

    /// <summary>
    /// The agent: the model, the ErabliereAI prompt under the function invocation
    /// layer, and the catalog's tools run as the caller.
    /// </summary>
    private static ChatClientAgent CreateAgent(IServiceProvider serviceProvider)
    {
        var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();
        var toolOptions = serviceProvider.GetRequiredService<IOptions<ErabliereAiToolOptions>>().Value;
        var catalog = serviceProvider.GetRequiredService<ErabliereAiToolCatalog>();

        var chatClient = serviceProvider.GetRequiredKeyedService<IChatClient>(AgentName)
            .AsBuilder()
            .UseFunctionInvocation(configure: functionInvocation =>
            {
                functionInvocation.MaximumIterationsPerRequest = toolOptions.MaxRounds;
            })
            .Use(inner => new ErabliereCopilotPromptChatClient(inner, httpContextAccessor))
            .Build(serviceProvider);

        IList<AITool> tools = toolOptions.Enabled ?
            [.. catalog.Functions.Select(f => new RequestScopedErabliereAiTool(f, httpContextAccessor))] :
            [];

        return new ChatClientAgent(
            chatClient,
            name: AgentName,
            description: "ErabliereAI, exposed over AG-UI for CopilotKit.",
            tools: tools,
            loggerFactory: serviceProvider.GetService<ILoggerFactory>(),
            services: serviceProvider);
    }
}
