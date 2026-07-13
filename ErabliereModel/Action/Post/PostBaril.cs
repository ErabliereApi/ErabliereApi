using System;
using System.ComponentModel.DataAnnotations;

namespace ErabliereApi.Donnees.Action.Post;

/// <summary>
/// Modèle de création d'un baril. Ne contient volontairement aucune propriété de navigation
/// afin d'empêcher l'over-posting vers des entités liées.
/// </summary>
public class PostBaril
{
    /// <summary>
    /// L'id guid si le client désire initialiser l'id
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// La clé étrangère de l'érablière possédant le baril
    /// </summary>
    public Guid? IdErabliere { get; set; }

    /// <summary>
    /// Date où le baril a été fermé
    /// </summary>
    public DateTimeOffset? DF { get; set; }

    /// <summary>
    /// Estimation de la qualité du sirop
    /// </summary>
    [MaxLength(15)]
    public string? QE { get; set; }

    /// <summary>
    /// Qualité du sirop après classement
    /// </summary>
    [MaxLength(15)]
    public string? Q { get; set; }
}
