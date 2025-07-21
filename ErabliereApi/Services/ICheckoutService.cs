using ErabliereApi.Donnees;
using Stripe;

namespace ErabliereApi.Services;

/// <summary>
/// 
/// </summary>
public interface ICheckoutService
{
    /// <summary>
    /// 
    /// </summary>
    Task<object> CreateSessionAsync(CancellationToken token);

    /// <summary>
    /// 
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
