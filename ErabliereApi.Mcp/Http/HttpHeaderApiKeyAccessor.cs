using Microsoft.AspNetCore.Http;

namespace ErabliereApi.Mcp.Http;

/// <summary>
/// Returns the api key sent by the MCP client on the current HTTP request. Used
/// by the HTTP transport, where the server is shared and holds no key of its own.
/// </summary>
/// <remarks>
/// Reading the key through <see cref="IHttpContextAccessor"/> is what the MCP SDK
/// sanctions: <c>HttpServerTransportOptions.PerSessionExecutionContext</c> is left
/// at its default of <c>false</c>, so a tool handler runs with the
/// <see cref="HttpContext"/> of the request that carried the <c>tools/call</c>.
/// <para>
/// This accessor is a singleton on purpose. <c>IHttpClientFactory</c> builds the
/// message handlers in a handler scope of its own rather than in the request
/// scope, and <see cref="IHttpContextAccessor"/>, being backed by an
/// <c>AsyncLocal</c>, is the one way to cross that boundary.
/// </para>
/// </remarks>
public class HttpHeaderApiKeyAccessor : IApiKeyAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Creates the accessor.
    /// </summary>
    public HttpHeaderApiKeyAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public string? GetApiKey()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext is null)
        {
            return null;
        }

        return httpContext.Request.Headers.TryGetValue(ApiKeyHandler.XApiKeyHeader, out var values)
            ? values.FirstOrDefault()
            : null;
    }
}
