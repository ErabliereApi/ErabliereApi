namespace ErabliereApi.Services.StripeIntegration;

/// <summary>
/// Classe représentant une utilisation avec une clé d'API
/// </summary>
public class Usage
{
    /// <summary>
    /// CustomerId de la clé d'API coté stripe
    /// </summary>
    public string CustomerId { get; set; } = "";

    /// <summary>
    /// SubscriptionId de la clé d'API coté stripe
    /// </summary>
    public string SubscriptionId { get; set; } = "";

    /// <summary>
    /// Quantité d'utilisation
    /// </summary>
    public int Quantite { get; set; }
}