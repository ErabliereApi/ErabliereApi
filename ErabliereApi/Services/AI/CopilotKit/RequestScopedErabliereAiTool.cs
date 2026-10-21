using ErabliereApi.Mcp.Serialization;
using ErabliereApi.Services.AI.Tools;
using Microsoft.Extensions.AI;
using System.Text.Json;

namespace ErabliereApi.Services.AI.CopilotKit;

/// <summary>
/// One tool of <see cref="ErabliereAiToolCatalog" />, made callable by an agent that
/// outlives the request it serves.
/// </summary>
/// <remarks>
/// The CopilotKit agent is a singleton, and the function invocation layer of
/// Microsoft.Extensions.AI hands the tools the service provider it was built with,
/// not the one of the request. The catalog's tools resolve <c>IErabliereAPIProxy</c>
/// from that provider, and the proxy must be the one of the request: its http client
/// forwards the credentials of the caller (see <see cref="CallerCredentialsHandler" />).
/// <para>
/// So the call is not run here: it is handed to the <see cref="IErabliereAiToolset" />
/// of the request being served, the very code path of the ErabliereAI chat, timeout,
/// error wording and logging included. The AI still holds no credential of its own.
/// </para>
/// </remarks>
public sealed class RequestScopedErabliereAiTool : DelegatingAIFunction
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly JsonElement _schema;

    /// <summary>
    /// Wraps a tool of the catalog.
    /// </summary>
    public RequestScopedErabliereAiTool(AIFunction catalogFunction, IHttpContextAccessor httpContextAccessor)
        : base(catalogFunction)
    {
        _httpContextAccessor = httpContextAccessor;
        _schema = ErabliereAiToolCatalog.ModelSchema(catalogFunction);
    }

    /// <inheritdoc />
    public override JsonElement JsonSchema => _schema;

    /// <inheritdoc />
    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        var requestServices = _httpContextAccessor.HttpContext?.RequestServices
            ?? throw new InvalidOperationException(
                $"The tool '{Name}' was called outside of an http request: there is no caller to read the data as.");

        var toolset = requestServices.GetRequiredService<IErabliereAiToolset>();

        var callId = FunctionInvokingChatClient.CurrentContext?.CallContent.CallId ?? Guid.NewGuid().ToString("N");

        var result = await toolset.InvokeAsync(
            new AIToolCall(callId, Name, JsonSerializer.Serialize(arguments, ToolJson.Options)),
            cancellationToken);

        // Already serialized by the toolset; handing back an element rather than the
        // string keeps the model from reading the json escaped a second time.
        using var json = JsonDocument.Parse(result.ResultJson);

        return json.RootElement.Clone();
    }
}
