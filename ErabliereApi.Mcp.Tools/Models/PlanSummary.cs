using System.Text.Json.Serialization;

namespace ErabliereApi.Mcp.Models;

/// <summary>
/// What the account behind the api key is subscribed to, and what that plan opens.
/// </summary>
public record PlanSummary
{
    /// <summary>
    /// The current plan name, for example <c>gratuit</c> or <c>base</c>.
    /// </summary>
    [JsonPropertyName("plan")]
    public required string Plan { get; init; }

    /// <summary>
    /// True when an active subscription backs the plan, false when the account has
    /// none and falls back to the free plan.
    /// </summary>
    [JsonPropertyName("activeSubscription")]
    public required bool ActiveSubscription { get; init; }

    /// <summary>
    /// Billing frequency of a paid plan, null for a free one.
    /// </summary>
    [JsonPropertyName("billingFrequency")]
    public string? BillingFrequency { get; init; }

    /// <summary>
    /// Start date of the active subscription, if any.
    /// </summary>
    [JsonPropertyName("startDate")]
    public DateTimeOffset? StartDate { get; init; }

    /// <summary>
    /// End date of the active subscription, null when it has no term.
    /// </summary>
    [JsonPropertyName("endDate")]
    public DateTimeOffset? EndDate { get; init; }

    /// <summary>
    /// The capabilities this plan is granted by the server configuration.
    /// </summary>
    [JsonPropertyName("capabilities")]
    public required IReadOnlyList<string> Capabilities { get; init; }

    /// <summary>
    /// Whether the plan opens the MCP server. Always true when the plan gate is off.
    /// </summary>
    [JsonPropertyName("mcpAccess")]
    public required bool McpAccess { get; init; }

    /// <summary>
    /// Whether this server restricts access by plan at all. False for a self-hosted
    /// stdio server, and for a hosted one whose operator has not turned the gate on.
    /// </summary>
    [JsonPropertyName("planGateEnabled")]
    public required bool PlanGateEnabled { get; init; }
}
