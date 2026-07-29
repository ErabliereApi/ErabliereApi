using ErabliereApi.Mcp.Models;

namespace ErabliereApi.Mcp.Services;

/// <summary>
/// Resolves the subscription plan of the account behind the api key of the current
/// request.
/// </summary>
public interface ISubscriptionPlanResolver
{
    /// <summary>
    /// Returns the current plan.
    /// </summary>
    /// <exception cref="SubscriptionPlanUnavailableException">
    /// The plan could not be established: the key was refused, the account could not
    /// be identified, or ErabliereAPI did not answer. The message is written for the
    /// end user and is safe to show them.
    /// </exception>
    Task<SubscriptionPlan> GetCurrentPlanAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Thrown when the subscription plan cannot be established. Carries a message
/// meant to be read by the person on the other end of the MCP client.
/// </summary>
public class SubscriptionPlanUnavailableException : Exception
{
    /// <summary>
    /// Creates the exception.
    /// </summary>
    public SubscriptionPlanUnavailableException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates the exception with the failure that caused it.
    /// </summary>
    public SubscriptionPlanUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
