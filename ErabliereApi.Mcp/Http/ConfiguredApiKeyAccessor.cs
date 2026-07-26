using ErabliereApi.Mcp.Configuration;
using Microsoft.Extensions.Options;

namespace ErabliereApi.Mcp.Http;

/// <summary>
/// Returns the api key configured through <c>ERABLIEREAPI_APIKEY</c>. Used by
/// the stdio transport, where the MCP client owns the process and passes the key
/// in the environment.
/// </summary>
public class ConfiguredApiKeyAccessor : IApiKeyAccessor
{
    private readonly IOptions<ErabliereApiMcpOptions> _options;

    /// <summary>
    /// Creates the accessor.
    /// </summary>
    public ConfiguredApiKeyAccessor(IOptions<ErabliereApiMcpOptions> options)
    {
        _options = options;
    }

    /// <inheritdoc />
    public string? GetApiKey() => _options.Value.ApiKey;
}
