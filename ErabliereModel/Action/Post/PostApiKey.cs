using System;
using System.ComponentModel.DataAnnotations;

namespace ErabliereApi.Donnees;

/// <summary>
/// Modèle de données pour créer une clé d'API.
/// </summary>
public class PostApiKey
{
    /// <summary>
    /// ID de l'utilisateur.
    /// </summary>
    [Required]
    public Guid? CustomerId { get; set; }

    /// <summary>
    /// Nom de la clé d'API.
    /// </summary>
    [MaxLength(50)]
    public string? Name { get; set; }

    /// <summary>
    /// Uri path autorisé
    /// </summary>
    [MaxLength(500)]
    public string? AuthorizeUris { get; set; }

    /// <summary>
    /// Autorisé les verbes
    /// </summary>
    [MaxLength(25)]
    public string? AuthorizeVerbs { get; set; }
}