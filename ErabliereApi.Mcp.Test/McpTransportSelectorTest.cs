using ErabliereApi.Mcp.Hosting;
using Shouldly;

namespace ErabliereApi.Mcp.Test;

/// <summary>
/// stdio is the transport of the MCP clients that already start this executable,
/// so a change of default is a breaking change for every one of them.
/// </summary>
public class McpTransportSelectorTest
{
    [Fact]
    public void Resolve_WithNoArgumentAndNoVariable_IsStdio()
    {
        McpTransportSelector.Resolve([], null).ShouldBe(McpTransportMode.Stdio);
    }

    [Theory]
    [InlineData("--http")]
    [InlineData("--HTTP")]
    public void Resolve_WithTheHttpSwitch_IsHttp(string argument)
    {
        McpTransportSelector.Resolve([argument], null).ShouldBe(McpTransportMode.Http);
    }

    [Theory]
    [InlineData("http")]
    [InlineData("Http")]
    [InlineData(" http ")]
    public void Resolve_WithTheEnvironmentVariable_IsHttp(string value)
    {
        McpTransportSelector.Resolve([], value).ShouldBe(McpTransportMode.Http);
    }

    [Fact]
    public void Resolve_WithTheStdioSwitch_OverridesTheEnvironmentVariable()
    {
        // The container image sets the variable; an operator running the same
        // image interactively must still be able to ask for stdio.
        McpTransportSelector.Resolve(["--stdio"], "http").ShouldBe(McpTransportMode.Stdio);
    }

    [Fact]
    public void Resolve_WithAnUnknownValue_FallsBackToStdio()
    {
        McpTransportSelector.Resolve([], "grpc").ShouldBe(McpTransportMode.Stdio);
    }

    [Fact]
    public void StripTransportSwitches_RemovesOnlyTheTransportSwitches()
    {
        // A lone --http would make the command line configuration provider throw
        // "Unrecognized argument format", so it never reaches the host builder.
        McpTransportSelector.StripTransportSwitches(["--http", "--Logging:LogLevel:Default=Debug", "--stdio"])
                            .ShouldBe(["--Logging:LogLevel:Default=Debug"]);
    }
}
