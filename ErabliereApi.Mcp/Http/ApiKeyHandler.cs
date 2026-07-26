namespace ErabliereApi.Mcp.Http;

/// <summary>
/// Adds the ErabliereAPI api key to every outgoing request.
/// The header name matches ErabliereApi.Authorization.ApiKeyAuthorization.ApiKeyMiddleware.XApiKeyHeader.
/// </summary>
public class ApiKeyHandler : DelegatingHandler
{
    /// <summary>
    /// Header read by the ErabliereAPI api key middleware. The MCP server reads
    /// the very same header when it is the one being called over HTTP, so a
    /// client configures one header name for both hops.
    /// </summary>
    public const string XApiKeyHeader = "X-ErabliereApi-ApiKey";

    private readonly IApiKeyAccessor _apiKeyAccessor;

    /// <summary>
    /// Creates the handler.
    /// </summary>
    public ApiKeyHandler(IApiKeyAccessor apiKeyAccessor)
    {
        _apiKeyAccessor = apiKeyAccessor;
    }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var apiKey = _apiKeyAccessor.GetApiKey();

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            // Calling ErabliereAPI without a key would come back as an opaque 401
            // several layers down; say what is missing instead.
            throw new InvalidOperationException(
                $"No ErabliereAPI api key is available for this call. In HTTP mode the MCP client must send the {XApiKeyHeader} header; in stdio mode the {Configuration.ErabliereApiMcpOptions.ApiKeyEnvironmentVariable} environment variable must be set.");
        }

        // Removed first: the retry handler replays the very same request message,
        // and adding the header twice would send two values.
        request.Headers.Remove(XApiKeyHeader);
        request.Headers.TryAddWithoutValidation(XApiKeyHeader, apiKey);

        return base.SendAsync(request, cancellationToken);
    }
}
