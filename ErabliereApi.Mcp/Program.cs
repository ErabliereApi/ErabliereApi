using ErabliereApi.Mcp.Hosting;

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
