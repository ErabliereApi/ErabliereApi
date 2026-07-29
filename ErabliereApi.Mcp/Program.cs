using ErabliereApi.Mcp.Hosting;

namespace ErabliereApi.Mcp;

/// <summary>
/// Entry point of the MCP server executable.
/// </summary>
/// <remarks>
/// Written as an explicit class rather than with top level statements: ErabliereApi
/// references this project for its tool set, and the <c>Program</c> class the
/// compiler generates for top level statements would then collide with the one of
/// the API (CS0436).
/// </remarks>
internal static class McpProgram
{
    private static async Task<int> Main(string[] args)
    {
        // stdio is the default: an MCP client that starts this executable with no
        // argument keeps getting the child process server of phases 1 and 2. HTTP is
        // opt-in with --http or ERABLIEREAPI_MCP_TRANSPORT=http.
        var transportMode = McpTransportSelector.Resolve(
            args,
            Environment.GetEnvironmentVariable(McpTransportSelector.TransportEnvironmentVariable));

        // The transport switches never reach the host builder: the command line
        // configuration provider expects --key=value pairs and throws on a lone --http.
        var hostArgs = McpTransportSelector.StripTransportSwitches(args);

        return transportMode switch
        {
            McpTransportMode.Http => await HttpServerRunner.RunAsync(hostArgs),
            _ => await StdioServerRunner.RunAsync(hostArgs)
        };
    }
}
