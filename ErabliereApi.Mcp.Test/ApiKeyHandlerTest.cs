using System.Net;
using ErabliereApi.Mcp.Configuration;
using ErabliereApi.Mcp.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Shouldly;

namespace ErabliereApi.Mcp.Test;

/// <summary>
/// The api key handler is the only piece of authentication code in the MCP
/// server, so its header must match the one read by ErabliereAPI.
/// </summary>
public class ApiKeyHandlerTest
{
    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<string[]> ReceivedApiKeys { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ReceivedApiKeys.Add(request.Headers.TryGetValues(ApiKeyHandler.XApiKeyHeader, out var values)
                ? [.. values]
                : []);

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    private static (HttpMessageInvoker Invoker, RecordingHandler Recorder) CreateInvoker(string apiKey)
    {
        return CreateInvoker(new ConfiguredApiKeyAccessor(Options.Create(new ErabliereApiMcpOptions { ApiKey = apiKey })));
    }

    private static (HttpMessageInvoker Invoker, RecordingHandler Recorder) CreateInvoker(IApiKeyAccessor accessor)
    {
        var recorder = new RecordingHandler();
        var handler = new ApiKeyHandler(accessor)
        {
            InnerHandler = recorder
        };

        return (new HttpMessageInvoker(handler), recorder);
    }

    [Fact]
    public async Task SendAsync_AddsTheApiKeyHeader()
    {
        var (invoker, recorder) = CreateInvoker("my-api-key");

        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://localhost/Erablieres"), CancellationToken.None);

        recorder.ReceivedApiKeys.Single().ShouldBe(["my-api-key"]);
    }

    [Fact]
    public async Task SendAsync_WhenTheSameRequestIsReplayed_DoesNotDuplicateTheHeader()
    {
        // The retry handler resends the very same HttpRequestMessage.
        var (invoker, recorder) = CreateInvoker("my-api-key");
        var request = new HttpRequestMessage(HttpMethod.Get, "https://localhost/Erablieres");

        await invoker.SendAsync(request, CancellationToken.None);
        await invoker.SendAsync(request, CancellationToken.None);

        recorder.ReceivedApiKeys.Count.ShouldBe(2);
        recorder.ReceivedApiKeys.ShouldAllBe(values => values.Length == 1);
    }

    [Fact]
    public async Task SendAsync_WhenTheAccessorHasNoKey_SaysWhichHeaderOrVariableIsMissing()
    {
        // The HTTP transport reaches this when a request slips past the api key
        // middleware, or when the accessor cannot see the HttpContext. An empty
        // header sent to ErabliereAPI would come back as an opaque 401.
        var (invoker, recorder) = CreateInvoker(new EmptyApiKeyAccessor());

        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://localhost/Erablieres"), CancellationToken.None));

        exception.Message.ShouldContain(ApiKeyHandler.XApiKeyHeader);
        exception.Message.ShouldContain(ErabliereApiMcpOptions.ApiKeyEnvironmentVariable);
        recorder.ReceivedApiKeys.ShouldBeEmpty();
    }

    [Fact]
    public void HttpHeaderApiKeyAccessor_ReadsTheKeyOfTheCurrentRequest()
    {
        // Over HTTP each caller brings its own key, so the accessor must read the
        // request rather than the configuration.
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[ApiKeyHandler.XApiKeyHeader] = "caller-key";

        var accessor = new HttpHeaderApiKeyAccessor(new HttpContextAccessor { HttpContext = httpContext });

        accessor.GetApiKey().ShouldBe("caller-key");
    }

    [Fact]
    public void HttpHeaderApiKeyAccessor_WhenThereIsNoRequest_ReturnsNull()
    {
        new HttpHeaderApiKeyAccessor(new HttpContextAccessor()).GetApiKey().ShouldBeNull();
    }

    private sealed class EmptyApiKeyAccessor : IApiKeyAccessor
    {
        public string? GetApiKey() => null;
    }
}
