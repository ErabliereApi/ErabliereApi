using Microsoft.AspNetCore.Http;

namespace ErabliereApi.Mcp.Http;

/// <summary>
/// Rejects the calls to the MCP endpoint that carry no ErabliereAPI api key.
/// </summary>
/// <remarks>
/// The check sits in front of the whole endpoint rather than inside the tools, so
/// <c>initialize</c> and <c>tools/list</c> are gated too: the tool catalog names
/// the maple grove features of an account and is not public.
/// <para>
/// The key itself is never validated here. ErabliereAPI owns the api key table
/// and answers 401/403 on the first tool call, which keeps a single authority for
/// revocation and usage tracking.
/// </para>
/// </remarks>
public class RequireApiKeyMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Creates the middleware.
    /// </summary>
    public RequireApiKeyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Lets the request through when the api key header is present.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var apiKey = context.Request.Headers.TryGetValue(ApiKeyHandler.XApiKeyHeader, out var values)
            ? values.FirstOrDefault()
            : null;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers.WWWAuthenticate = $"ApiKey header=\"{ApiKeyHandler.XApiKeyHeader}\"";
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsync(
                $"{{\"error\":\"The {ApiKeyHandler.XApiKeyHeader} header is required. Create an api key in the ErabliereAPI web application under Profil -> Clés d'api.\"}}");

            return;
        }

        await _next(context);
    }
}
