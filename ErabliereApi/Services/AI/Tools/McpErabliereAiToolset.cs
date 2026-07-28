using ErabliereApi.Extensions;
using ErabliereApi.Mcp.Models;
using ErabliereApi.Mcp.Serialization;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using OpenAI.Chat;
using System.Diagnostics;
using System.Text.Json;

namespace ErabliereApi.Services.AI.Tools;

/// <summary>
/// Default <see cref="IErabliereAiToolset" />, running the MCP tools in process.
/// </summary>
/// <remarks>
/// The tools read ErabliereAPI through <c>IErabliereAPIProxy</c>, and the instance
/// they get is the one registered for the request being served, whose http client
/// carries the credentials of the caller (see <see cref="CallerCredentialsHandler" />).
/// That is what makes the AI unable to reach data its user could not reach by hand:
/// it holds no credential of its own, so every read is subject to the same
/// authorization pipeline as a call the user would make themselves.
/// </remarks>
public class McpErabliereAiToolset : IErabliereAiToolset
{
    private readonly ErabliereAiToolCatalog _catalog;
    private readonly IServiceProvider _requestServices;
    private readonly IOptions<ErabliereAiToolOptions> _options;
    private readonly ILogger<McpErabliereAiToolset> _logger;

    /// <summary>
    /// Constructeur par initialisation
    /// </summary>
    public McpErabliereAiToolset(
        ErabliereAiToolCatalog catalog,
        IServiceProvider requestServices,
        IOptions<ErabliereAiToolOptions> options,
        ILogger<McpErabliereAiToolset> logger)
    {
        _catalog = catalog;
        _requestServices = requestServices;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public IReadOnlyList<ChatTool> GetChatTools()
    {
        return _catalog.ChatTools;
    }

    /// <inheritdoc />
    public async Task<ErabliereAiToolResult> InvokeAsync(AIToolCall toolCall, CancellationToken token)
    {
        var function = _catalog.Find(toolCall.FunctionName);

        if (function is null)
        {
            // Listing what does exist turns a dead end into a round the model can
            // recover from.
            return Error(toolCall,
                $"There is no tool named '{toolCall.FunctionName}'. The available tools are: {string.Join(", ", _catalog.ToolNames)}.");
        }

        if (!TryParseArguments(toolCall.FunctionArguments, out var arguments, out var parseError))
        {
            return Error(toolCall, parseError);
        }

        arguments.Services = _requestServices;

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(_options.Value.ToolTimeout);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await function.InvokeAsync(arguments, timeout.Token);

            var json = JsonSerializer.Serialize(result, ToolJson.Options);

            _logger.LogInformation("ErabliereAI tool {Tool} answered in {Elapsed} ms.",
                toolCall.FunctionName.Sanatize(), stopwatch.ElapsedMilliseconds);

            return new ErabliereAiToolResult(
                toolCall.Id,
                toolCall.FunctionName,
                toolCall.FunctionArguments,
                json,
                IsError: false,
                EstimateTokens(json));
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // The user gave up on the whole request; there is nobody left to answer.
            throw;
        }
        catch (Exception exception)
        {
            if (timeout.IsCancellationRequested)
            {
                _logger.LogWarning("ErabliereAI tool {Tool} timed out after {Timeout}.",
                    toolCall.FunctionName.Sanatize(), _options.Value.ToolTimeout);

                return Error(toolCall,
                    $"The tool '{toolCall.FunctionName}' took longer than {_options.Value.ToolTimeout.TotalSeconds:0} seconds and was abandoned. Narrow the query, for example by asking for a shorter date range.");
            }

            // A tool failure is data for the model, not a failed request: it may ask
            // for the right identifier, narrow the range, or answer without the tool.
            var message = ToModelReadableMessage(exception);

            _logger.LogWarning(exception, "ErabliereAI tool {Tool} failed.", toolCall.FunctionName.Sanatize());

            return Error(toolCall, message);
        }
    }

    /// <summary>
    /// Turns an exception into one sentence the model can act on.
    /// </summary>
    /// <remarks>
    /// <see cref="McpException" /> is what the tools throw on purpose and its message
    /// is already written for a model; the chain is walked because the invocation
    /// layer wraps what a tool body throws. Anything else is unexpected here, and its
    /// message may carry connection strings or internal urls, so nothing of it is
    /// disclosed.
    /// </remarks>
    private static string ToModelReadableMessage(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is McpException mcpException)
            {
                return mcpException.Message;
            }
        }

        return "This tool could not be executed because of an unexpected error on the server. Answer with what you already know, and tell the user the live data could not be read.";
    }

    private static bool TryParseArguments(string? argumentsJson, out AIFunctionArguments arguments, out string error)
    {
        arguments = new AIFunctionArguments();
        error = "";

        if (string.IsNullOrWhiteSpace(argumentsJson))
        {
            return true;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argumentsJson, ToolJson.Options);

            if (parsed is null)
            {
                return true;
            }

            foreach (var entry in parsed)
            {
                arguments[entry.Key] = entry.Value;
            }

            return true;
        }
        catch (JsonException exception)
        {
            error = $"The arguments of this call are not a valid json object ({exception.Message}). Send the arguments again as a json object matching the tool schema.";

            return false;
        }
    }

    private static ErabliereAiToolResult Error(AIToolCall toolCall, string message)
    {
        var json = JsonSerializer.Serialize(new { error = message }, ToolJson.Options);

        return new ErabliereAiToolResult(
            toolCall.Id,
            toolCall.FunctionName,
            toolCall.FunctionArguments,
            json,
            IsError: true,
            EstimateTokens(json));
    }

    private static int EstimateTokens(string json)
    {
        return json.Length / ToolResponse.CharactersPerToken;
    }
}
