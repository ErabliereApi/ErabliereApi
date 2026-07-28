namespace ErabliereApi.Services.AI;

/// <summary>
/// Provider agnostic result of a chat completion.
/// </summary>
public class AIResponse
{
    /// <summary>
    /// The text content of the answer.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// The refusal message, when the model refused to answer.
    /// </summary>
    public string? Refusal { get; set; }

    /// <summary>
    /// The kind of the first content part, see OpenAI.Chat.ChatMessageContentPartKind.
    /// </summary>
    public string? Kind { get; set; }

    /// <summary>
    /// The uri of the generated image, when the content part is an image.
    /// </summary>
    public Uri? ImageUri { get; set; }

    /// <summary>
    /// Why the model stopped generating, see OpenAI.Chat.ChatFinishReason.
    /// Normalized accross providers so a tool calling loop can branch on it.
    /// </summary>
    public string? FinishReason { get; set; }

    /// <summary>
    /// The tools the model asked to call, empty when the model answered directly.
    /// The execution of those calls is not handled here.
    /// </summary>
    public IReadOnlyList<AIToolCall> ToolCalls { get; set; } = [];
}

/// <summary>
/// Provider agnostic tool call requested by the model.
/// </summary>
/// <param name="Id">The identifier used to correlate the call with its result.</param>
/// <param name="FunctionName">The name of the function to invoke.</param>
/// <param name="FunctionArguments">The arguments of the call, as a JSON object.</param>
public record AIToolCall(string Id, string FunctionName, string FunctionArguments);
