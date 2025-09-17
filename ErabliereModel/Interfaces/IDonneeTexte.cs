namespace ErabliereModel.Interfaces;

/// <summary>
/// Interface for data that can provide a text message to write in SMS or email.
/// </summary>
public interface IDonneeTexte
{
    /// <summary>
    /// Retrieves the text data associated with the current instance.
    /// </summary>
    /// <returns>A string containing the text data, or <see langword="null"/> if no text data is available.</returns>
    public string? GetDonneeTexte();
}