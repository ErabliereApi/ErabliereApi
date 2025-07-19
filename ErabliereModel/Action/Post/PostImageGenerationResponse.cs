using System.Collections.Generic;

namespace ErabliereApi.Donnees.Action.Post;

/// <summary>
/// Réponse pour la génération d'images
/// </summary>
public class PostImageGenerationResponse
{
    /// <summary>
    /// Liste des images générées
    /// </summary>
    public List<PostImageGenerationResponseImage> Images { get; set; } = new List<PostImageGenerationResponseImage>();
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
}