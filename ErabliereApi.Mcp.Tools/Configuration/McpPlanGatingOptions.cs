namespace ErabliereApi.Mcp.Configuration;

/// <summary>
/// Restriction of the HTTP transport by subscription plan.
/// </summary>
/// <remarks>
/// The plan to capability mapping is configuration, never code: the offer changes
/// with the business, and adding a plan or opening MCP to an existing one must not
/// need a rebuild. A capability is an arbitrary string, so the same section can gate
/// something other than MCP later on without a second shape to learn.
/// <para>
/// The section is <c>Mcp:PlanGating</c> of <c>appsettings.json</c>, overridable with
/// the usual environment variables (<c>Mcp__PlanGating__Enabled=true</c>).
/// </para>
/// </remarks>
public class McpPlanGatingOptions
{
    /// <summary>
    /// Configuration section the options are bound from.
    /// </summary>
    public const string SectionName = "Mcp:PlanGating";

    /// <summary>
    /// Capability a plan must be granted for its holder to reach the MCP endpoint.
    /// </summary>
    public const string McpCapability = "mcp";

    /// <summary>
    /// Turns the gate on. Off by default, on purpose: an existing deployment keeps
    /// answering every caller until an operator decides otherwise, the same
    /// progressive rollout the API applies with <c>Abonnement.ValiderPlan</c>.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Capability looked up in <see cref="PlanCapabilities"/> to allow a call.
    /// </summary>
    public string RequiredCapability { get; set; } = McpCapability;

    /// <summary>
    /// Plan reported for a user whose account carries no active subscription.
    /// Kept configurable so that "no subscription" and "free plan" can be told
    /// apart should a deployment ever want to.
    /// </summary>
    public string DefaultPlan { get; set; } = "gratuit";

    /// <summary>
    /// What each plan is allowed to do, keyed by the plan name ErabliereAPI returns
    /// (<c>ErabliereApi.Donnees.ForfaitsAbonnement</c>). A plan absent from the map
    /// is granted nothing.
    /// </summary>
    public Dictionary<string, string[]> PlanCapabilities { get; set; } = [];

    /// <summary>
    /// How long a resolved plan is reused before ErabliereAPI is asked again. An MCP
    /// session sends many requests, and a plan changes at most a few times a year;
    /// without this every <c>tools/call</c> would cost an extra round trip and a
    /// database query. The trade-off is that a cancellation takes up to this long to
    /// close the door. Set to zero to disable the cache.
    /// </summary>
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Optional url shown in the denial message, where a user subscribes.
    /// </summary>
    public string? SubscriptionUrl { get; set; }

    /// <summary>
    /// The capabilities granted to a plan, case-insensitively. Empty when the plan is
    /// unknown to the configuration.
    /// </summary>
    /// <remarks>
    /// The lookup cannot rely on the dictionary comparer: the configuration binder
    /// builds a plain <see cref="Dictionary{TKey, TValue}"/> with the default ordinal
    /// comparer, whatever comparer this property is initialized with.
    /// </remarks>
    public IReadOnlyList<string> CapabilitiesFor(string? plan)
    {
        if (string.IsNullOrWhiteSpace(plan))
        {
            return [];
        }

        foreach (var entry in PlanCapabilities)
        {
            if (string.Equals(entry.Key, plan, StringComparison.OrdinalIgnoreCase))
            {
                return entry.Value ?? [];
            }
        }

        return [];
    }

    /// <summary>
    /// Whether a plan may reach the MCP endpoint.
    /// </summary>
    public bool GrantsAccess(string? plan)
    {
        return CapabilitiesFor(plan)
            .Any(capability => string.Equals(capability, RequiredCapability, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// The plans that do grant access, in configuration order. Used to tell a denied
    /// user what to subscribe to rather than only what they lack.
    /// </summary>
    public IReadOnlyList<string> PlansGrantingAccess()
    {
        return PlanCapabilities
            .Where(entry => entry.Value != null &&
                            entry.Value.Any(capability => string.Equals(capability, RequiredCapability, StringComparison.OrdinalIgnoreCase)))
            .Select(entry => entry.Key)
            .ToArray();
    }

    /// <summary>
    /// Validates the options and returns the error messages, if any. Only meaningful
    /// when the gate is on: a disabled gate is never read.
    /// </summary>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        if (!Enabled)
        {
            return errors;
        }

        if (string.IsNullOrWhiteSpace(RequiredCapability))
        {
            errors.Add($"{SectionName}:RequiredCapability is required when the plan gate is enabled.");
        }

        if (CacheDuration < TimeSpan.Zero)
        {
            errors.Add($"{SectionName}:CacheDuration cannot be negative.");
        }

        // A gate no plan can pass locks every caller out, including the operator who
        // turned it on. Refusing to start says so, instead of answering the same
        // denial to everyone until someone reads the configuration.
        if (!string.IsNullOrWhiteSpace(RequiredCapability) && PlansGrantingAccess().Count == 0)
        {
            errors.Add($"{SectionName}:PlanCapabilities grants '{RequiredCapability}' to no plan, so no one could ever connect. " +
                       $"Add the capability to at least one plan, or set {SectionName}:Enabled to false.");
        }

        return errors;
    }
}
