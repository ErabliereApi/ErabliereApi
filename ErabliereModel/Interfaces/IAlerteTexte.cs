namespace ErabliereModel.Interfaces;

/// <summary>
/// Interface for alert that can provide a text message to write in SMS or email.
/// </summary>
public interface IAlerteTexte
{
    /// <summary>
    /// Gets the alert text associated with the current instance.
    /// </summary>
    public string? GetAlerteTexte();
}