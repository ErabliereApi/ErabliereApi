using ErabliereApi.Mcp.Configuration;
using Shouldly;

namespace ErabliereApi.Mcp.Test;

/// <summary>
/// The server is started by an MCP client with only environment variables, so a
/// misconfiguration must produce a message the user can act on.
/// </summary>
public class ErabliereApiMcpOptionsTest
{
    private static ErabliereApiMcpOptions ValidOptions() => new()
    {
        BaseUrl = "https://erabliereapi.freddycoder.com",
        ApiKey = "an-api-key"
    };

    [Fact]
    public void Validate_WhenEverythingIsSet_ReturnsNoError()
    {
        ValidOptions().Validate().ShouldBeEmpty();
    }

    [Fact]
    public void Validate_WhenNothingIsSet_NamesBothEnvironmentVariables()
    {
        var errors = new ErabliereApiMcpOptions().Validate();

        errors.Count.ShouldBe(2);
        errors.ShouldContain(error => error.Contains(ErabliereApiMcpOptions.BaseUrlEnvironmentVariable));
        errors.ShouldContain(error => error.Contains(ErabliereApiMcpOptions.ApiKeyEnvironmentVariable));
    }

    [Theory]
    [InlineData("erabliereapi.freddycoder.com")]
    [InlineData("/Erablieres")]
    [InlineData("ftp://erabliereapi.freddycoder.com")]
    public void Validate_WhenTheUrlIsNotAnAbsoluteHttpUrl_ReturnsAnError(string baseUrl)
    {
        var options = ValidOptions();
        options.BaseUrl = baseUrl;

        options.Validate().ShouldContain(error => error.Contains("absolute http or https url"));
    }

    [Fact]
    public void Validate_WhenMaxRetriesIsZero_ReturnsAnError()
    {
        var options = ValidOptions();
        options.MaxRetries = 0;

        options.Validate().ShouldContain(error => error.Contains("MaxRetries"));
    }
}
