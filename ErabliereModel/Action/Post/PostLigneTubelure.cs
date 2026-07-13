using System;
using System.ComponentModel.DataAnnotations;

namespace ErabliereApi.Donnees.Action.Post;

/// <summary>
/// Modèle de création d'une ligne de tubelure. Ne contient volontairement aucune
/// propriété de navigation afin d'empêcher l'over-posting vers des entités liées.
/// </summary>
public class PostLigneTubelure
{
    /// <summary>
    /// L'id guid si le client désire initialiser l'id
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// La clé étrangère de l'érablière
    /// </summary>
    public Guid? IdErabliere { get; set; }

    /// <summary>
    /// Le nom de la ligne (ex: M-01)
    /// </summary>
    [MaxLength(100, ErrorMessage = "Le nom de la ligne ne peut pas dépasser 100 caractères.")]
    public string? Nom { get; set; }

    /// <summary>
    /// Le type de la ligne. Voir <see cref="Contantes.TypeLigneTubelure" /> pour les valeurs permises.
    /// </summary>
    [MaxLength(20, ErrorMessage = "Le type de la ligne ne peut pas dépasser 20 caractères.")]
    public string? TypeLigne { get; set; }

    /// <summary>
    /// Les coordonnées de la ligne sous forme de tableau JSON de paires [longitude, latitude],
    /// dans l'ordre du tracé. Ex: [[-71.25, 46.82], [-71.26, 46.83]]
    /// </summary>
    public string? CoordonneesJson { get; set; }
}
