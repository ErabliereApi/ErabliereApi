namespace ErabliereApi.Donnees.Action.Post;

/// <summary>
/// Réponse pour la génération d'images
/// </summary>
public class PostImageGenerationResponse
{
    /// <summary>
    /// Liste des images générées
    /// </summary>
    public PostImageGenerationResponseImage[] Images { get; set; } = [];

    /// <summary>
    /// Message d'erreur, s'il y en a un
    /// </summary>
    public string? Erreur { get; set; }
}

/// <summary>
/// Représente une image générée
/// </summary>
public class PostImageGenerationResponseImage
{
    /// <summary>
    /// URL de l'image générée
    /// </summary>
    public string Url { get; set; } = "";

    /// <summary>
    /// Message d'erreur, s'il y en a un
    /// </summary>
    public string? Erreur { get; set; }
}