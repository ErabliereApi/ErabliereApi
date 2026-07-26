using System.ComponentModel;
using ErabliereApi.Mcp.Configuration;
using ErabliereApi.Mcp.Models;
using ErabliereApi.Mcp.Services;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace ErabliereApi.Mcp.Tools;

/// <summary>
/// Tool reporting the subscription plan of the account behind the api key.
/// </summary>
/// <remarks>
/// It exists so a user can find out what they have access to from inside their MCP
/// client, instead of learning it only when a call is refused.
/// </remarks>
[McpServerToolType]
public static class PlanTools
{
    /// <summary>
    /// Gets the current subscription plan and what it opens.
    /// </summary>
    [McpServerTool(Name = "get_my_plan", ReadOnly = true, Idempotent = true)]
    [Description("Gets the ErabliereAPI subscription plan of the account owning the API key, whether it is backed by an active subscription, and what that plan grants on this server. " +
                 "Use this to answer 'what plan am I on', 'do I have access to this MCP server', or to explain why a call was refused for lack of a subscription. " +
                 "Returns an envelope {summary, data, truncated}.")]
    public static async Task<ToolResponse<PlanSummary>> GetMyPlanAsync(
        ISubscriptionPlanResolver planResolver,
        IOptions<McpPlanGatingOptions> gatingOptions,
        CancellationToken cancellationToken = default)
    {
        var gating = gatingOptions.Value;

        SubscriptionPlan plan;

        try
        {
            plan = await planResolver.GetCurrentPlanAsync(cancellationToken);
        }
        catch (SubscriptionPlanUnavailableException exception)
        {
            // The message of this exception is written for the user; a stack trace
            // would tell them nothing they can act on.
            throw new McpException(exception.Message);
        }

        var capabilities = gating.CapabilitiesFor(plan.Plan);
        var mcpAccess = !gating.Enabled || gating.GrantsAccess(plan.Plan);

        var summary = new PlanSummary
        {
            Plan = plan.Plan,
            ActiveSubscription = plan.AbonnementActif,
            BillingFrequency = plan.FrequenceFacturation,
            StartDate = plan.DateDebut,
            EndDate = plan.DateFin,
            Capabilities = capabilities,
            McpAccess = mcpAccess,
            PlanGateEnabled = gating.Enabled
        };

        return ToolResponse.ForItem(BuildSummary(gating, summary), summary);
    }

    private static string BuildSummary(McpPlanGatingOptions gating, PlanSummary summary)
    {
        var subscription = summary.ActiveSubscription
            ? $"Plan '{summary.Plan}', backed by an active subscription"
            : $"Plan '{summary.Plan}', with no active subscription on the account";

        if (!string.IsNullOrWhiteSpace(summary.BillingFrequency))
        {
            subscription += $" billed {summary.BillingFrequency}";
        }

        if (!summary.PlanGateEnabled)
        {
            return $"{subscription}. This MCP server does not restrict access by plan, so every plan may use it.";
        }

        if (summary.McpAccess)
        {
            return $"{subscription}. It grants access to this MCP server.";
        }

        var plansGrantingAccess = gating.PlansGrantingAccess();

        var required = plansGrantingAccess.Count == 1
            ? $"the '{plansGrantingAccess[0]}' plan"
            : $"one of these plans: {string.Join(", ", plansGrantingAccess)}";

        var message = $"{subscription}. It does not grant access to this MCP server, which requires {required}.";

        return string.IsNullOrWhiteSpace(gating.SubscriptionUrl)
            ? message
            : $"{message} Subscribe at {gating.SubscriptionUrl}.";
    }
}
