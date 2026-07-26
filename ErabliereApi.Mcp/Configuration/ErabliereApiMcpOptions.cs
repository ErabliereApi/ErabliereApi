using ErabliereApi.Mcp.Hosting;

namespace ErabliereApi.Mcp.Configuration;

/// <summary>
/// Options used to reach the ErabliereAPI instance the MCP server exposes.
/// Every value comes from an environment variable, because an MCP client
/// (Claude Code, Claude Desktop, ...) starts the server as a child process
/// and can only pass configuration through the environment.
/// </summary>
public class ErabliereApiMcpOptions
{
    /// <summary>
    /// Name of the named <see cref="HttpClient"/> used by the proxy.
    /// </summary>
    public const string HttpClientName = "ErabliereAPI";

    /// <summary>
    /// Environment variable holding the base url of the API, for example https://erabliereapi.freddycoder.com.
    /// </summary>
    public const string BaseUrlEnvironmentVariable = "ERABLIEREAPI_URL";

    /// <summary>
    /// Environment variable holding the api key sent in the X-ErabliereApi-ApiKey header.
    /// </summary>
    public const string ApiKeyEnvironmentVariable = "ERABLIEREAPI_APIKEY";

    /// <summary>
    /// Base url of the ErabliereAPI instance.
    /// </summary>
    public string BaseUrl { get; set; } = "";

    /// <summary>
    /// Api key created from the ErabliereAPI user interface (Profil -> Clés d'api).
    /// Required by the stdio transport only: over HTTP each MCP client sends its
    /// own key on every request.
    /// </summary>
    public string ApiKey { get; set; } = "";

    /// <summary>
    /// Number of attempts performed by the retry handler before giving up.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Delay between two attempts. Kept short because an MCP client is waiting
    /// synchronously for the tool result.
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Validates the options and returns the error messages, if any.
    /// </summary>
    /// <param name="transportMode">
    /// Transport the server is about to start with. Over HTTP the api key is not
    /// configuration at all: it travels on each incoming request, so requiring
    /// one here would force an operator to invent a key the server never uses.
    /// </param>
    public IReadOnlyList<string> Validate(McpTransportMode transportMode = McpTransportMode.Stdio)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(BaseUrl))
        {
            errors.Add($"The environment variable {BaseUrlEnvironmentVariable} is required.");
        }
        else if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri) ||
                 (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            errors.Add($"The environment variable {BaseUrlEnvironmentVariable} must be an absolute http or https url. Current value: '{BaseUrl}'.");
        }

        if (transportMode == McpTransportMode.Stdio && string.IsNullOrWhiteSpace(ApiKey))
        {
            errors.Add($"The environment variable {ApiKeyEnvironmentVariable} is required.");
        }

        if (MaxRetries < 1)
        {
            errors.Add("MaxRetries must be greater than or equal to 1.");
        }

        return errors;
    }
}
