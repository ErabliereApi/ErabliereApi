namespace ErabliereApi.Mcp.Hosting;

/// <summary>
/// Transport used to talk to the MCP client.
/// </summary>
public enum McpTransportMode
{
    /// <summary>
    /// The MCP client starts this server as a child process and exchanges the
    /// JSON-RPC frames over stdin/stdout. This is the default.
    /// </summary>
    Stdio,

    /// <summary>
    /// The server is hosted and the MCP client reaches it over the Streamable
    /// HTTP transport.
    /// </summary>
    Http
}
