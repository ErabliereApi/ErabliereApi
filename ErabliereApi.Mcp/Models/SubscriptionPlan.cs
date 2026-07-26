using System.Text.Json.Serialization;

namespace ErabliereApi.Mcp.Models;

/// <summary>
/// The current subscription plan of the account behind the api key, as returned by
/// <c>GET /api/Abonnements/Courant</c>.
/// </summary>
/// <remarks>
/// The property names match the French ones ErabliereAPI serializes, because this
/// is the wire contract. <c>ErabliereAPI.Proxy</c> has no method for this endpoint:
/// it is generated from the OpenAPI document with NSwag Studio and predates the
/// subscription feature, so the call is made with a plain <c>HttpClient</c> until
/// the proxy is regenerated.
/// </remarks>
public record SubscriptionPlan
{
    /// <summary>
    /// The plan name, for example <c>gratuit</c> or <c>base</c>.
    /// </summary>
    [JsonPropertyName("plan")]
    public string Plan { get; init; } = "";

    /// <summary>
    /// True when an active subscription is behind the plan, false when the free plan
    /// is what an account without any subscription defaults to.
    /// </summary>
    [JsonPropertyName("abonnementActif")]
    public bool AbonnementActif { get; init; }

    /// <summary>
    /// Start date of the active subscription, if any.
    /// </summary>
    [JsonPropertyName("dateDebut")]
    public DateTimeOffset? DateDebut { get; init; }

    /// <summary>
    /// End date of the active subscription, null when it has no term.
    /// </summary>
    [JsonPropertyName("dateFin")]
    public DateTimeOffset? DateFin { get; init; }

    /// <summary>
    /// Billing frequency of a paid plan (<c>mensuelle</c>, <c>annuelle</c>), null for
    /// a free one.
    /// </summary>
    [JsonPropertyName("frequenceFacturation")]
    public string? FrequenceFacturation { get; init; }
}
