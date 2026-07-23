using ErabliereApi.Donnees;
using ErabliereApi.Donnees.Action.Post;
using ErabliereApi.Services.StripeIntegration;

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
    /// Create a checkout session for a user account subscription, billed at a
    /// fixed frequency (see <see cref="FrequencesFacturation" />) as opposed to
    /// the usage-based billing of API keys.
    /// </summary>
    /// <param name="frequenceFacturation">The billing frequency chosen by the user</param>
    /// <param name="token">Cancellation token</param>
    Task<PostCheckoutObjResponse> CreateAbonnementSessionAsync(string frequenceFacturation, CancellationToken token);

    /// <summary>
    /// Get the customer's subscription status
    /// </summary>
    Task<object?> GetCustomerSubscriptionStatusAsync(CancellationToken token);

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
    Task<object> GetProjectBalanceAsync(CancellationToken token);

    /// <summary>
    /// Get the current state of usage records.
    /// Those are not sent to Stripe yet.
    /// </summary>
    IEnumerable<Usage> GetUsageRecords();

    /// <summary>
    /// Get the customer's upcoming invoice
    /// </summary>
    Task<object> GetCustomerUpcomingInvoiceAsync(string subscriptionId, CancellationToken token);

    /// <summary>
    /// Cancel a subscription on the payment provider side
    /// </summary>
    Task CancelSubscriptionAsync(string subscriptionId, CancellationToken token);
}
