namespace ErabliereApi.Services.AI.Tools;

/// <summary>
/// Tells whether the caller's subscription plan opens the live data tools to the
/// chat.
/// </summary>
public interface IErabliereAiCapabilityService
{
    /// <summary>
    /// Resolves the tool capabilities of the user of the request being served.
    /// </summary>
    Task<ErabliereAiCapabilities> GetCapabilitiesAsync(CancellationToken token);
}

/// <summary>
/// What the chat may do for a given user.
/// </summary>
/// <param name="ToolsEnabled">
/// True when the model may call the read-only tools. False brings back the chat of
/// phase 7, which answers from the model's own knowledge only.
/// </param>
/// <param name="Plan">The plan the user is currently on.</param>
/// <param name="PlanGateEnabled">Whether the deployment restricts the tools by plan at all.</param>
/// <param name="PlansGrantingAccess">The plans that do open the tools, to tell a denied user what to subscribe to.</param>
/// <param name="SubscriptionUrl">Where to subscribe, when the deployment configured one.</param>
public sealed record ErabliereAiCapabilities(
    bool ToolsEnabled,
    string Plan,
    bool PlanGateEnabled,
    IReadOnlyList<string> PlansGrantingAccess,
    string? SubscriptionUrl);
