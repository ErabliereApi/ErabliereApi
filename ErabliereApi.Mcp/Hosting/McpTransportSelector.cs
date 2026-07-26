namespace ErabliereApi.Mcp.Hosting;

/// <summary>
/// Reads the transport to use from the command line and from the environment.
/// </summary>
/// <remarks>
/// stdio stays the default: an MCP client that already starts this executable
/// without any argument must keep getting a stdio server.
/// </remarks>
public static class McpTransportSelector
{
    /// <summary>
    /// Environment variable holding <c>http</c> or <c>stdio</c>. Used by the
    /// container image, where an entrypoint argument is easy to lose.
    /// </summary>
    public const string TransportEnvironmentVariable = "ERABLIEREAPI_MCP_TRANSPORT";

    private const string HttpSwitch = "--http";
    private const string StdioSwitch = "--stdio";

    /// <summary>
    /// Resolves the transport. The command line wins over the environment.
    /// </summary>
    /// <param name="args">Arguments the process was started with.</param>
    /// <param name="environmentValue">Value of <see cref="TransportEnvironmentVariable"/>.</param>
    public static McpTransportMode Resolve(string[] args, string? environmentValue)
    {
        foreach (var argument in args)
        {
            if (IsTransportSwitch(argument))
            {
                return string.Equals(argument, HttpSwitch, StringComparison.OrdinalIgnoreCase)
                    ? McpTransportMode.Http
                    : McpTransportMode.Stdio;
            }
        }

        return string.Equals(environmentValue?.Trim(), "http", StringComparison.OrdinalIgnoreCase)
            ? McpTransportMode.Http
            : McpTransportMode.Stdio;
    }

    /// <summary>
    /// Removes the transport switches from the arguments.
    /// </summary>
    /// <remarks>
    /// The command line configuration provider expects <c>--key=value</c> or
    /// <c>--key value</c> pairs and throws on a lone <c>--http</c>, so the
    /// switches never reach the host builder.
    /// </remarks>
    public static string[] StripTransportSwitches(string[] args)
    {
        return [.. args.Where(argument => !IsTransportSwitch(argument))];
    }

    private static bool IsTransportSwitch(string argument)
    {
        return string.Equals(argument, HttpSwitch, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(argument, StdioSwitch, StringComparison.OrdinalIgnoreCase);
    }
}
