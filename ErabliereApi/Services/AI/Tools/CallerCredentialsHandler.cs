using ErabliereApi.Authorization;
using ErabliereApi.Extensions;
using Microsoft.Net.Http.Headers;

namespace ErabliereApi.Services.AI.Tools;

/// <summary>
/// Copies the credentials of the request being served onto the calls the
/// ErabliereAI tools make back to ErabliereAPI.
/// </summary>
/// <remarks>
/// This handler is the whole security story of the feature. The AI is given no
/// credential of its own — no service account, no api key, no elevated token — so a
/// tool call is authenticated as, and only as, the user who typed the prompt. Every
/// ownership rule the API already enforces therefore applies unchanged, and there is
/// no code path through which the chat could read an érablière its user cannot read.
/// <para>
/// When the request carried no credential at all, the call is refused here rather
/// than being sent anonymously: a tool answering "no maple grove found" because the
/// call lost its identity would be indistinguishable from an empty account.
/// </para>
/// </remarks>
public class CallerCredentialsHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly LoopbackRequestMarker _loopbackMarker;

    /// <summary>
    /// Constructeur par initialisation
    /// </summary>
    public CallerCredentialsHandler(IHttpContextAccessor httpContextAccessor, LoopbackRequestMarker loopbackMarker)
    {
        _httpContextAccessor = httpContextAccessor;
        _loopbackMarker = loopbackMarker;
    }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var incoming = _httpContextAccessor.HttpContext?.Request
            ?? throw new InvalidOperationException(
                "An ErabliereAI tool tried to read data outside of an http request, so no caller could be identified. Tools may only run while serving a user request.");

        // Tells the pipeline this call is nested inside a request the process is
        // already serving, so nothing queues it behind its own caller.
        _loopbackMarker.Mark(request);

        var forwarded = false;

        if (incoming.Headers.TryGetValue(HeaderNames.Authorization, out var authorization) &&
            !string.IsNullOrWhiteSpace(authorization))
        {
            // Removed first: a retry replays the very same request message, and the
            // header would otherwise be sent twice.
            request.Headers.Remove(HeaderNames.Authorization);
            request.Headers.TryAddWithoutValidation(HeaderNames.Authorization, authorization.ToString());
            forwarded = true;
        }

        if (incoming.Headers.TryGetValue(ApiKeyMiddleware.XApiKeyHeader, out var apiKey) &&
            !string.IsNullOrWhiteSpace(apiKey))
        {
            request.Headers.Remove(ApiKeyMiddleware.XApiKeyHeader);
            request.Headers.TryAddWithoutValidation(ApiKeyMiddleware.XApiKeyHeader, apiKey.ToString());
            forwarded = true;
        }

        if (!forwarded && UsesAuthentication())
        {
            throw new InvalidOperationException(
                "The request being served carries no credential to forward, so no ErabliereAI tool call can be attributed to a user. The call was not sent.");
        }

        return base.SendAsync(request, cancellationToken);
    }

    /// <summary>
    /// Whether the deployment authenticates its callers at all. With
    /// USE_AUTHENTICATION off every request is anonymous by design, and refusing to
    /// forward nothing would break the local and demo setups.
    /// </summary>
    private bool UsesAuthentication()
    {
        var configuration = _httpContextAccessor.HttpContext?.RequestServices.GetService<IConfiguration>();

        return configuration is not null && configuration.IsAuthEnabled();
    }
}
