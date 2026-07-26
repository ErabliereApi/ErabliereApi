using ErabliereApi.Mcp.Tools;
using ModelContextProtocol;
using Shouldly;

namespace ErabliereApi.Mcp.Test;

/// <summary>
/// Validation of the arguments a language model can send to the tools.
/// </summary>
public class ToolArgumentsTest
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ParseId_WhenValueIsMissing_ThrowsMcpException(string? value)
    {
        var exception = Should.Throw<McpException>(() => ToolArguments.ParseId(value, "erabliereId"));

        exception.Message.ShouldContain("erabliereId");
        exception.Message.ShouldContain("required");
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("42")]
    [InlineData("3fa85f64-5717-4562-b3fc")]
    public void ParseId_WhenValueIsNotAGuid_ThrowsMcpException(string value)
    {
        var exception = Should.Throw<McpException>(() => ToolArguments.ParseId(value, "erabliereId"));

        exception.Message.ShouldContain("must be a GUID");
    }

    [Fact]
    public void ParseId_WhenValueIsTheEmptyGuid_ThrowsMcpException()
    {
        var exception = Should.Throw<McpException>(() => ToolArguments.ParseId(Guid.Empty.ToString(), "erabliereId"));

        exception.Message.ShouldContain("empty GUID");
    }

    [Fact]
    public void ParseId_WhenValueIsAGuid_ReturnsIt()
    {
        var expected = Guid.NewGuid();

        // The surrounding whitespaces are tolerated, models often add them.
        ToolArguments.ParseId($"  {expected}  ", "erabliereId").ShouldBe(expected);
    }

    [Fact]
    public void ValidateTop_WhenNotSpecified_ReturnsTheDefault()
    {
        ToolArguments.ValidateTop(null).ShouldBe(ToolArguments.DefaultTop);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(ToolArguments.MaxTop)]
    public void ValidateTop_WhenInRange_ReturnsTheValue(int top)
    {
        ToolArguments.ValidateTop(top).ShouldBe(top);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(ToolArguments.MaxTop + 1)]
    public void ValidateTop_WhenOutOfRange_ThrowsMcpException(int top)
    {
        var exception = Should.Throw<McpException>(() => ToolArguments.ValidateTop(top));

        exception.Message.ShouldContain("between 1 and");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void BuildContainsFilter_WhenNoSearchTerm_ReturnsNull(string? searchTerm)
    {
        ToolArguments.BuildContainsFilter("nom", searchTerm).ShouldBeNull();
    }

    [Fact]
    public void BuildContainsFilter_WhenSearchTerm_BuildsAnODataFilter()
    {
        ToolArguments.BuildContainsFilter("nom", " Sucrerie ").ShouldBe("contains(nom,'Sucrerie')");
    }

    [Fact]
    public void BuildContainsFilter_WhenSearchTermContainsAQuote_EscapesIt()
    {
        // Without the escaping, the term would close the OData string literal
        // and the rest would be interpreted as an expression.
        ToolArguments.BuildContainsFilter("nom", "l'Érable' or true eq true and contains(nom,'")
                     .ShouldBe("contains(nom,'l''Érable'' or true eq true and contains(nom,''')");
    }
}
