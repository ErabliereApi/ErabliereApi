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
}