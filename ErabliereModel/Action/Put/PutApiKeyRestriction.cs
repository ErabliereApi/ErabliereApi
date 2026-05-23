using System.ComponentModel.DataAnnotations;

namespace ErabliereApi.Donnees.Action.Put;

/// <summary>
/// Représente une requête pour mettre à jour les restrictions d'une clé d'API.
/// </summary>
public class PutApiKeyRestriction
{
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