using System.ClientModel;

namespace ErabliereApi.Services.AI;

/// <summary>
/// Thrown when the LLM rejects a chat completion request.
///
/// The underlying <see cref="ClientResultException" /> is kept so the HTTP layer
/// can translate the provider error into a problem details response.
/// </summary>
public class AIChatCompletionException : Exception
{
    /// <summary>
    /// Constructeur par initialisation
    /// </summary>
    /// <param name="innerException">The provider exception.</param>
    public AIChatCompletionException(ClientResultException innerException)
        : base(innerException.Message, innerException)
    {
        ClientResult = innerException;
    }

    /// <summary>
    /// The provider exception carrying the status code and error data.
    /// </summary>
    public ClientResultException ClientResult { get; }
}
