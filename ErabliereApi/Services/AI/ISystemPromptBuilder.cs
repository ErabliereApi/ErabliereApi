namespace ErabliereApi.Services.AI;

/// <summary>
/// Builds the system prompt used by ErabliereAI conversations.
///
/// This is the single place where the system prompt is composed. Enriching the
/// prompt (tool descriptions, erabliere context, ...) happens here and nowhere else.
/// </summary>
public interface ISystemPromptBuilder
{
    /// <summary>
    /// The system prompt used when neither the conversation nor the request
    /// specifies one.
    /// </summary>
    string DefaultSystemPrompt { get; }

    /// <summary>
    /// The system prompt to store on a conversation being created.
    /// </summary>
    /// <param name="requestedSystemMessage">The system message sent by the client, may be null or blank.</param>
    string BuildForNewConversation(string? requestedSystemMessage);

    /// <summary>
    /// The system prompt to send to the LLM for an existing conversation.
    /// </summary>
    /// <param name="conversationSystemMessage">The system message stored on the conversation, may be null or blank.</param>
    string BuildForCompletion(string? conversationSystemMessage);

    /// <summary>
    /// The system prompt to send to the LLM, enriched with what the model needs to
    /// know about the tools it may call and about the maple grove the user is
    /// looking at.
    /// </summary>
    /// <param name="conversationSystemMessage">The system message stored on the conversation, may be null or blank.</param>
    /// <param name="context">What the request says about the tools and the current maple grove.</param>
    string BuildForCompletion(string? conversationSystemMessage, ErabliereAiPromptContext context);
}

/// <summary>
/// What a prompt knows about its surroundings, beyond the words the user typed.
/// </summary>
/// <param name="ToolsEnabled">
/// True when the read-only tools are offered to the model on this prompt.
/// </param>
/// <param name="ErabliereId">
/// The maple grove the chat was opened from, when it was opened from one.
/// </param>
/// <param name="ErabliereNom">
/// The name of that maple grove, only ever used to make the prompt readable.
/// </param>
/// <param name="Capteurs">
/// The sensors of that maple grove, read before the model was called. Empty when
/// there is no maple grove in context or when the read failed, in which case the
/// model lists them itself.
/// </param>
/// <remarks>
/// <paramref name="ErabliereId" /> and <paramref name="ErabliereNom" /> come from the
/// client and are never an authorization input: they are written into the prompt so
/// the model stops guessing identifiers, and a caller who sends an identifier they do
/// not own gains nothing, because the tool call it leads to is still authenticated as
/// them and refused by the API.
/// <para>
/// <paramref name="Capteurs" /> is not client supplied: it is read from the API as the
/// caller, so an identifier they do not own yields an empty list rather than the names
/// of somebody else's sensors.
/// </para>
/// </remarks>
public readonly record struct ErabliereAiPromptContext(
    bool ToolsEnabled,
    Guid? ErabliereId = null,
    string? ErabliereNom = null,
    IReadOnlyList<ErabliereAiCapteur>? Capteurs = null)
{
    /// <summary>
    /// A prompt with no tools and no maple grove in sight, the phase 7 behaviour.
    /// </summary>
    public static ErabliereAiPromptContext None => new(false);
}

/// <summary>
/// One sensor, reduced to what the model needs to recognize it in a question and
/// then read it.
/// </summary>
/// <param name="Id">Identifier to pass to get_donnees_capteur.</param>
/// <param name="Nom">Name its owner gave it, which is what the model has to match against.</param>
/// <param name="Symbole">Unit of its readings, null when it has none.</param>
/// <param name="Online">True when it was still reporting at the last check.</param>
public readonly record struct ErabliereAiCapteur(
    Guid Id,
    string Nom,
    string? Symbole = null,
    bool? Online = null);
