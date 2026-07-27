using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;

namespace ErabliereApi.Mcp.Http;

/// <summary>
/// Writes a JSON-RPC error response on the MCP endpoint.
/// </summary>
/// <remarks>
/// A bare HTTP status would be swallowed: an MCP client turns a non-2xx answer into
/// a transport failure whose body is usually dropped, so the user reads "the server
/// returned 403" and never the sentence explaining what to subscribe to. A
/// <c>200</c> carrying a JSON-RPC error object travels the path the protocol
/// defines for application-level failures, and the client surfaces the message as
/// the error of the call. Operators still get the reason on the
/// <see cref="DeniedReasonHeader"/> response header, which is the convention the API
/// itself follows with <c>X-ErabliereApi-ForbidenReason</c>.
/// </remarks>
public static class JsonRpcErrorWriter
{
    /// <summary>
    /// Header carrying the denial reason, for logs and monitoring.
    /// </summary>
    public const string DeniedReasonHeader = "X-ErabliereApi-Mcp-Denied-Reason";

    /// <summary>
    /// Error code of a call refused because the subscription plan does not allow it.
    /// Inside the -32000..-32099 range JSON-RPC reserves for implementation defined
    /// server errors, so it can never collide with a code of the protocol itself.
    /// </summary>
    public const int PlanRequiredErrorCode = -32003;

    /// <summary>
    /// Error code of a call refused because the plan could not be established.
    /// </summary>
    public const int PlanUnavailableErrorCode = -32004;

    /// <summary>
    /// Writes the error, echoing the id of the request being answered when it can be
    /// read from the body.
    /// </summary>
    public static async Task WriteAsync(
        HttpContext context,
        int errorCode,
        string message,
        IReadOnlyDictionary<string, object?>? data = null)
    {
        var id = await ReadRequestIdAsync(context);

        var error = new JsonObject
        {
            ["code"] = errorCode,
            ["message"] = message
        };

        if (data != null)
        {
            var dataNode = new JsonObject();

            foreach (var entry in data)
            {
                dataNode[entry.Key] = entry.Value == null ? null : JsonValue.Create(entry.Value);
            }

            error["data"] = dataNode;
        }

        var payload = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id,
            ["error"] = error
        };

        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "application/json";
        context.Response.Headers[DeniedReasonHeader] = Sanitize(message);

        await context.Response.WriteAsync(payload.ToJsonString(), context.RequestAborted);
    }

    /// <summary>
    /// Reads the <c>id</c> of the JSON-RPC request in the body, or null when there is
    /// none to read: a GET opening a stream, a notification, or a body this server
    /// never got to parse. A null id is what JSON-RPC prescribes for an error raised
    /// before the request could be identified.
    /// </summary>
    private static async Task<JsonNode?> ReadRequestIdAsync(HttpContext context)
    {
        if (!HttpMethods.IsPost(context.Request.Method))
        {
            return null;
        }

        try
        {
            // Nothing downstream will read the body: the request is being refused
            // here, so consuming the stream costs nothing.
            using var document = await JsonDocument.ParseAsync(context.Request.Body, cancellationToken: context.RequestAborted);

            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty("id", out var idElement))
            {
                return null;
            }

            return idElement.ValueKind switch
            {
                JsonValueKind.String => JsonValue.Create(idElement.GetString()),
                JsonValueKind.Number when idElement.TryGetInt64(out var number) => JsonValue.Create(number),
                _ => null
            };
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException)
        {
            return null;
        }
    }

    /// <summary>
    /// Keeps the header value to the printable ASCII a header may carry: the message
    /// holds plan names that come from configuration.
    /// </summary>
    private static string Sanitize(string message)
    {
        var sanitized = new string(message.Where(c => c is >= ' ' and <= '~').ToArray());

        return sanitized.Length > 400 ? sanitized[..400] : sanitized;
    }
}
