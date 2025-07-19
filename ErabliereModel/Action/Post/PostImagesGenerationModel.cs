using System.ComponentModel.DataAnnotations;

namespace ErabliereApi.Donnees.Action.Post;

/// <summary>
/// Modèle pour la génération d'images
/// </summary>
public class PostImagesGenerationModel
{
    /// <summary>
    /// Nom du déploiement pour la génération d'images. Si aucun nom n'est fourni, le modèle par défaut sera utilisé.
    /// </summary>
    public string? DeploymentName { get; set; }

    /// <summary>
    /// Nombre d'images à générer
    /// </summary>
    public int? ImageCount { get; set; }

    /// <summary>
    /// La requête pour la génération d'image
    /// </summary>
    public string Prompt { get; set; } = "";

    /// <summary>
    /// Taille des images
    /// </summary>
    /// <example>1024x1024</example>
    [RegularExpression("^(1024x1024|1792x1024|1024x1792)$", ErrorMessage = "La taille devrait être une des suivantes '1024x1024', '1792x1024', '1024x1792'.")]
    public string? Size { get; set; }

    /// <summary>
    /// Qualité des images
    /// </summary>
    [RegularExpression("^(Standard|Hd)$", ErrorMessage = "La qualité devrait être une des suivantes 'Standard', 'HD'.")]
    public string? Quality { get; set; }

    /// <summary>
    /// Style des images
    /// </summary>
    [RegularExpression("^(Natural|Vivid)$", ErrorMessage = "Le style devrait être une des suivantes 'Natural', 'Vivid'.")]
    public string? Style { get; set; }
    
}
