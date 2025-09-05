using ErabliereApi.Extensions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace ErabliereApi.Test.Extension;

public class HttpContextExtensionTest
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("127.0.0.1", "127.0.0.1")]
    public void GetClientIp_ShouldReturn_WhenNoXForwardedForHeader(string ipInContext, string expectedValue)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.HttpContext.Connection.RemoteIpAddress = ipInContext == null ? null : System.Net.IPAddress.Parse(ipInContext);
        
        // Act
        var ip = context.GetClientIp();
        
        // Assert
        Assert.Equal(expectedValue, ip);
    }

    [Fact]
    public void GetClientIp_ShouldReturn_WhenXForwardedForHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.HttpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("::ffff:172.17.0.1");
        context.Request.HttpContext.Request.Headers["X-Forwarded-For"] = "172.105.102.10";

        // Act
        var ip = context.GetClientIp();

        // Assert
        Assert.Equal("172.105.102.10", ip);
    }
}
