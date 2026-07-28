using OpenAI.Chat;

namespace ErabliereApi.Services.AI.Tools;

/// <summary>
/// The read-only tool set ErabliereAI may call to answer a question with the
/// real data of the user asking it.
/// </summary>
/// <remarks>
/// Registered scoped: an invocation runs with the services of the request being
/// served, which is what binds a tool call to the credentials of the caller.
/// </remarks>
public interface IErabliereAiToolset
{
    /// <summary>
    /// The tool definitions to hand to the model, in the shape the chat completion
    /// api expects.
    /// </summary>
    IReadOnlyList<ChatTool> GetChatTools();

    /// <summary>
    /// Executes one tool call requested by the model.
    /// </summary>
    /// <remarks>
    /// Never throws for a failure the model could recover from: an unknown tool, a
    /// bad argument, a refused query or a timeout all come back as a result whose
    /// <see cref="ErabliereAiToolResult.IsError" /> is true and whose json says what
    /// went wrong, so the model can correct itself on the next round.
    /// </remarks>
    Task<ErabliereAiToolResult> InvokeAsync(AIToolCall toolCall, CancellationToken token);
}

/// <summary>
/// Outcome of one tool call.
/// </summary>
/// <param name="ToolCallId">Identifier correlating the result with the call the model made.</param>
/// <param name="ToolName">Name of the tool that was asked for.</param>
/// <param name="ArgumentsJson">The arguments the model produced, as a json object.</param>
/// <param name="ResultJson">The json handed back to the model.</param>
/// <param name="IsError">True when <paramref name="ResultJson" /> describes a failure rather than data.</param>
/// <param name="EstimatedTokens">What <paramref name="ResultJson" /> is expected to cost in the context window.</param>
public sealed record ErabliereAiToolResult(
    string ToolCallId,
    string ToolName,
    string ArgumentsJson,
    string ResultJson,
    bool IsError,
    int EstimatedTokens);
