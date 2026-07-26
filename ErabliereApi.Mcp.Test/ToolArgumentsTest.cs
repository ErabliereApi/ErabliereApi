using ErabliereApi.Mcp.Services;
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
    [InlineData("   ")]
    public void ParseOptionalDate_WhenValueIsMissing_ReturnsNull(string? value)
    {
        ToolArguments.ParseOptionalDate(value, "startDate").ShouldBeNull();
    }

    [Theory]
    [InlineData("not-a-date")]
    [InlineData("2026-13-45")]
    [InlineData("12/03/2026 or something")]
    public void ParseOptionalDate_WhenValueIsNotADate_ThrowsMcpException(string value)
    {
        var exception = Should.Throw<McpException>(() => ToolArguments.ParseOptionalDate(value, "startDate"));

        exception.Message.ShouldContain("startDate");
        exception.Message.ShouldContain("ISO 8601");
    }

    [Fact]
    public void ParseOptionalDate_WhenValueCarriesAnOffset_KeepsIt()
    {
        var date = ToolArguments.ParseOptionalDate("2026-03-12T06:30:00-04:00", "startDate");

        date.ShouldBe(new DateTimeOffset(2026, 3, 12, 6, 30, 0, TimeSpan.FromHours(-4)));
    }

    [Fact]
    public void ParseOptionalDate_WhenValueIsADayAlone_ReadsItAsLocalMidnight()
    {
        // A model writing "2026-03-12" means that day at the maple grove, so the
        // date must not silently become midnight UTC.
        var date = ToolArguments.ParseOptionalDate(" 2026-03-12 ", "startDate");

        date!.Value.Offset.ShouldBe(TimeZoneInfo.Local.GetUtcOffset(new DateTime(2026, 3, 12)));
        date.Value.DateTime.ShouldBe(new DateTime(2026, 3, 12, 0, 0, 0));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("2026-03-12", null)]
    [InlineData(null, "2026-03-13")]
    [InlineData("", "   ")]
    public void ParseRequiredDateRange_WhenABoundIsMissing_ThrowsMcpException(string? start, string? end)
    {
        var exception = Should.Throw<McpException>(() => ToolArguments.ParseRequiredDateRange(start, end));

        exception.Message.ShouldContain("both required");
        exception.Message.ShouldContain("ISO 8601");
    }

    [Fact]
    public void ParseRequiredDateRange_WhenTheRangeIsInverted_ThrowsMcpException()
    {
        var exception = Should.Throw<McpException>(() => ToolArguments.ParseRequiredDateRange("2026-03-13", "2026-03-12"));

        exception.Message.ShouldContain("must not be earlier than");
    }

    [Fact]
    public void ParseRequiredDateRange_WhenTheBoundsAreEqual_IsAccepted()
    {
        // A single instant is a legitimate, if empty, range: it is the API that
        // decides there is nothing in it, not this validation.
        var range = ToolArguments.ParseRequiredDateRange("2026-03-12T06:00:00-04:00", "2026-03-12T06:00:00-04:00");

        range.Duration.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public void ParseRequiredDateRange_WhenTheRangeIsValid_ReturnsIt()
    {
        var range = ToolArguments.ParseRequiredDateRange("2026-03-12T06:00:00-04:00", "2026-03-19T06:00:00-04:00");

        range.Start.ShouldBe(new DateTimeOffset(2026, 3, 12, 6, 0, 0, TimeSpan.FromHours(-4)));
        range.End.ShouldBe(new DateTimeOffset(2026, 3, 19, 6, 0, 0, TimeSpan.FromHours(-4)));
        range.Duration.ShouldBe(TimeSpan.FromDays(7));
    }

    [Fact]
    public void ParseOptionalDateRange_WhenBothBoundsAreMissing_ReturnsTwoNulls()
    {
        var (start, end) = ToolArguments.ParseOptionalDateRange(null, null);

        start.ShouldBeNull();
        end.ShouldBeNull();
    }

    [Fact]
    public void ParseOptionalDateRange_WhenOnlyOneBoundIsGiven_LeavesTheOtherSideUnbounded()
    {
        var (start, end) = ToolArguments.ParseOptionalDateRange("2026-03-12", null);

        start.ShouldNotBeNull();
        end.ShouldBeNull();
    }

    [Fact]
    public void ParseOptionalDateRange_WhenTheRangeIsInverted_ThrowsMcpException()
    {
        var exception = Should.Throw<McpException>(() => ToolArguments.ParseOptionalDateRange("2026-03-13", "2026-03-12"));

        exception.Message.ShouldContain("must not be earlier than");
    }

    [Fact]
    public void ValidateMaxPoints_WhenNotSpecified_ReturnsTheDefault()
    {
        ToolArguments.ValidateMaxPoints(null).ShouldBe(DonneesCapteurSummarizer.DefaultMaxSeriePoints);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(DonneesCapteurSummarizer.MaxSeriePointsLimit + 1)]
    public void ValidateMaxPoints_WhenOutOfRange_ThrowsMcpException(int maxPoints)
    {
        var exception = Should.Throw<McpException>(() => ToolArguments.ValidateMaxPoints(maxPoints));

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

    [Fact]
    public void BuildContainsAnyFilter_SearchesEveryProperty()
    {
        ToolArguments.BuildContainsAnyFilter(" entaille ", "title", "text")
                     .ShouldBe("contains(title,'entaille') or contains(text,'entaille')");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void BuildContainsAnyFilter_WhenNoSearchTerm_ReturnsNull(string? searchTerm)
    {
        ToolArguments.BuildContainsAnyFilter(searchTerm, "title", "text").ShouldBeNull();
    }

    [Fact]
    public void BuildContainsAnyFilter_WhenSearchTermContainsAQuote_EscapesItInEveryClause()
    {
        ToolArguments.BuildContainsAnyFilter("l'eau", "title", "text")
                     .ShouldBe("contains(title,'l''eau') or contains(text,'l''eau')");
    }
}
