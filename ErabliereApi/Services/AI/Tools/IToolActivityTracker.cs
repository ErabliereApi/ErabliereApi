namespace ErabliereApi.Services.AI.Tools;

/// <summary>
/// Publishes what the model is doing while a prompt is being answered, so the
/// chat can show "Consultation des données de capteurs…" instead of a spinner that
/// says nothing for twenty seconds.
/// </summary>
/// <remarks>
/// The client generates the identifier and sends it with the prompt, then polls the
/// status endpoint with it. Nothing here is authoritative — a lost status only costs
/// the label of a progress line, never the answer.
/// </remarks>
public interface IToolActivityTracker
{
    /// <summary>
    /// Records a step of the tool loop.
    /// </summary>
    /// <param name="activityId">Identifier the client sent with the prompt. Ignored when null.</param>
    /// <param name="step">The step to publish.</param>
    void Publish(Guid? activityId, ToolActivityStep step);

    /// <summary>
    /// Marks the loop as finished, so the client stops polling.
    /// </summary>
    void Complete(Guid? activityId);

    /// <summary>
    /// Reads the current activity, or null when nothing is known about this
    /// identifier.
    /// </summary>
    ToolActivity? Get(Guid activityId);
}

/// <summary>
/// One step of the tool loop.
/// </summary>
/// <param name="Round">The round of the loop, starting at one.</param>
/// <param name="ToolName">The tool being called, null while the model is thinking.</param>
/// <param name="Label">A sentence in French, ready to be displayed.</param>
public sealed record ToolActivityStep(int Round, string? ToolName, string Label);

/// <summary>
/// The activity of one prompt.
/// </summary>
/// <param name="Steps">Every step published so far, oldest first.</param>
/// <param name="Completed">True once the prompt has been answered.</param>
public sealed record ToolActivity(IReadOnlyList<ToolActivityStep> Steps, bool Completed);
