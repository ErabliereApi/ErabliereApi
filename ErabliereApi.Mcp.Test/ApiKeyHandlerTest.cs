using System.Net;
using ErabliereApi.Mcp.Configuration;
using ErabliereApi.Mcp.Http;
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
        var recorder = new RecordingHandler();
        var handler = new ApiKeyHandler(Options.Create(new ErabliereApiMcpOptions { ApiKey = apiKey }))
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
}
