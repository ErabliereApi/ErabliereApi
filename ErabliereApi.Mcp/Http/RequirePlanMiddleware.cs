using ErabliereApi.Mcp.Configuration;
using ErabliereApi.Mcp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ErabliereApi.Mcp.Http;

/// <summary>
/// Refuses the calls to the MCP endpoint made by an account whose subscription plan
/// does not include MCP access.
/// </summary>
/// <remarks>
/// It runs after <see cref="RequireApiKeyMiddleware"/>, so a key is known to be
/// present, and in front of the whole endpoint, so <c>initialize</c> and
/// <c>tools/list</c> are gated too: a plan that grants no MCP access should not even
/// see the tool catalog.
/// <para>
/// The HTTP transport only. A stdio server is started by the user on their own
/// machine with their own key and answers no one else; charging them a plan to run a
/// process they host would gate nothing.
/// </para>
/// <para>
/// It fails closed. When ErabliereAPI cannot be reached, the plan is unknown, and
/// letting an unknown plan through would make the gate a suggestion — a client
/// pointed at an unreachable API would gain the access the configuration denies it.
/// The message says the check failed rather than pretending the plan is wrong.
/// </para>
/// </remarks>
public class RequirePlanMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Creates the middleware.
    /// </summary>
    public RequirePlanMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Lets the request through when the caller's plan grants MCP access.
    /// </summary>
    public async Task InvokeAsync(
        HttpContext context,
        IOptions<McpPlanGatingOptions> options,
        ISubscriptionPlanResolver planResolver,
        ILogger<RequirePlanMiddleware> logger)
    {
        var gating = options.Value;

        if (!gating.Enabled)
        {
            await _next(context);

            return;
        }

        Models.SubscriptionPlan plan;

        try
        {
            plan = await planResolver.GetCurrentPlanAsync(context.RequestAborted);
        }
        catch (SubscriptionPlanUnavailableException exception)
        {
            logger.LogWarning(exception, "The subscription plan of a caller could not be resolved; the call is refused.");

            await JsonRpcErrorWriter.WriteAsync(
                context,
                JsonRpcErrorWriter.PlanUnavailableErrorCode,
                exception.Message);

            return;
        }

        if (gating.GrantsAccess(plan.Plan))
        {
            await _next(context);

            return;
        }

        var plansGrantingAccess = gating.PlansGrantingAccess();

        logger.LogInformation(
            "MCP access refused: the plan '{Plan}' does not grant the capability '{Capability}'.",
            plan.Plan, gating.RequiredCapability);

        await JsonRpcErrorWriter.WriteAsync(
            context,
            JsonRpcErrorWriter.PlanRequiredErrorCode,
            BuildDenialMessage(gating, plan, plansGrantingAccess),
            new Dictionary<string, object?>
            {
                ["currentPlan"] = plan.Plan,
                ["requiredCapability"] = gating.RequiredCapability,
                ["plansGrantingAccess"] = string.Join(", ", plansGrantingAccess),
                ["subscriptionUrl"] = gating.SubscriptionUrl
            });
    }

    private static string BuildDenialMessage(
        McpPlanGatingOptions gating,
        Models.SubscriptionPlan plan,
        IReadOnlyList<string> plansGrantingAccess)
    {
        var current = plan.AbonnementActif
            ? $"The subscription of this account is on the '{plan.Plan}' plan"
            : $"This account has no active subscription, so it is on the '{plan.Plan}' plan";

        var required = plansGrantingAccess.Count == 1
            ? $"the '{plansGrantingAccess[0]}' plan"
            : $"one of these plans: {string.Join(", ", plansGrantingAccess)}";

        var message = $"{current}, which does not include access to the ErabliereAPI MCP server. " +
                      $"Reaching it requires {required}.";

        if (!string.IsNullOrWhiteSpace(gating.SubscriptionUrl))
        {
            message += $" Subscribe at {gating.SubscriptionUrl}.";
        }

        return message;
    }
}
