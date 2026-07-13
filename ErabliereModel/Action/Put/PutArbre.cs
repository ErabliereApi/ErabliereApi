using System;
using System.ComponentModel.DataAnnotations;

namespace ErabliereApi.Donnees.Action.Put;

/// <summary>
/// Modèle de modification d'un arbre cartographié. Ne contient volontairement aucune
/// propriété de navigation afin d'empêcher l'over-posting vers des entités liées.
/// </summary>
public class PutArbre
{
    /// <summary>
    /// L'id de l'arbre à modifier
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// La clé étrangère de l'érablière
    /// </summary>
    public Guid? IdErabliere { get; set; }

    /// <summary>
    /// L'identifiant ou le nom de l'arbre
    /// </summary>
    [MaxLength(100, ErrorMessage = "Le nom de l'arbre ne peut pas dépasser 100 caractères.")]
    public string? Nom { get; set; }

    /// <summary>
    /// L'espèce de l'arbre
    /// </summary>
    [MaxLength(50, ErrorMessage = "L'espèce de l'arbre ne peut pas dépasser 50 caractères.")]
    public string? Espece { get; set; }

    /// <summary>
    /// La latitude de l'arbre
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// La longitude de l'arbre
    /// </summary>
    public double Longitude { get; set; }
}
