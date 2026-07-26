namespace ErabliereApi.Mcp.Http;

/// <summary>
/// Supplies the ErabliereAPI api key <see cref="ApiKeyHandler"/> puts on every
/// outgoing request.
/// </summary>
/// <remarks>
/// The two transports get the key from two different places: stdio reads it once
/// from the environment, HTTP reads it from the incoming request, because the
/// hosted server serves several callers and each one brings its own key.
/// </remarks>
public interface IApiKeyAccessor
{
    /// <summary>
    /// Returns the api key to use for the call being made, or <c>null</c> when
    /// none is available.
    /// </summary>
    string? GetApiKey();
}
