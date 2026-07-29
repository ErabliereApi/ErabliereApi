namespace ErabliereApi.Services.AI.Tools;

/// <summary>
/// Reads the sensors of a maple grove before the model is called, so the names it
/// has to match against are in front of it rather than guessed.
/// </summary>
/// <remarks>
/// This exists because of one failure the tools cannot avoid on their own. A sensor
/// is named by its owner — "Brix", "Vac. principale", "T° cabane" — and the search
/// parameter of <c>list_capteurs</c> is a case sensitive substring. A model asked
/// about "le degré Brix" searches those words, matches nothing, and concludes the
/// sensor does not exist. Handing it the real names removes both the guess and the
/// round trip it would have cost.
/// </remarks>
public interface IErabliereAiCapteurCatalog
{
    /// <summary>
    /// The sensors of a maple grove, empty when they could not be read.
    /// </summary>
    /// <param name="erabliereId">
    /// The maple grove the chat was opened from. It comes from the client, so the
    /// read is authenticated as the caller and answers empty rather than throwing
    /// when they do not own it.
    /// </param>
    /// <param name="token">Cancellation token of the request being served.</param>
    Task<IReadOnlyList<ErabliereAiCapteur>> ReadAsync(Guid erabliereId, CancellationToken token);
}
