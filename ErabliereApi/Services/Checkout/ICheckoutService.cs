using ErabliereApi.Donnees;
using ErabliereApi.Donnees.Action.Post;

namespace ErabliereApi.Services;

/// <summary>
/// Interface for the checkout service
/// </summary>
public interface ICheckoutService
{
    /// <summary>
    /// Create a checkout session
    /// </summary>
    Task<PostCheckoutObjResponse> CreateSessionAsync(CancellationToken token);

    /// <summary>
    /// Handle the webhook events from Stripe
    /// </summary>
    Task Webhook(string json);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="apiKey"></param>
    /// <returns></returns>
    Task ReccordUsageAsync(ApiKey apiKey);

    /// <summary>
    /// Get the balance of the Stripe account
    /// </summary>
    Task<object> GetBalanceAsync(CancellationToken token);
}
