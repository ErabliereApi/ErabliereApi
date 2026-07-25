using ErabliereApi.Mcp.Configuration;
using Microsoft.Extensions.Options;

namespace ErabliereApi.Mcp.Http;

/// <summary>
/// Adds the ErabliereAPI api key to every outgoing request.
/// The header name matches ErabliereApi.Authorization.ApiKeyAuthorization.ApiKeyMiddleware.XApiKeyHeader.
/// </summary>
public class ApiKeyHandler : DelegatingHandler
{
    /// <summary>
    /// Header read by the ErabliereAPI api key middleware.
    /// </summary>
    public const string XApiKeyHeader = "X-ErabliereApi-ApiKey";

    private readonly IOptions<ErabliereApiMcpOptions> _options;

    /// <summary>
    /// Creates the handler.
    /// </summary>
    public ApiKeyHandler(IOptions<ErabliereApiMcpOptions> options)
    {
        _options = options;
    }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Removed first: the retry handler replays the very same request message,
        // and adding the header twice would send two values.
        request.Headers.Remove(XApiKeyHeader);
        request.Headers.TryAddWithoutValidation(XApiKeyHeader, _options.Value.ApiKey);

        return base.SendAsync(request, cancellationToken);
    }
}
