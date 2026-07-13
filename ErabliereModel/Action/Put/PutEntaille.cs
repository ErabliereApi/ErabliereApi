using System;
using System.ComponentModel.DataAnnotations;

namespace ErabliereApi.Donnees.Action.Put;

/// <summary>
/// Modèle de modification d'une entaille cartographiée. Ne contient volontairement aucune
/// propriété de navigation afin d'empêcher l'over-posting vers des entités liées.
/// </summary>
public class PutEntaille
{
    /// <summary>
    /// L'id de l'entaille à modifier
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// La clé étrangère de l'érablière
    /// </summary>
    public Guid? IdErabliere { get; set; }

    /// <summary>
    /// L'identifiant ou le nom de l'entaille
    /// </summary>
    [MaxLength(100, ErrorMessage = "Le nom de l'entaille ne peut pas dépasser 100 caractères.")]
    public string? Nom { get; set; }

    /// <summary>
    /// La latitude de l'entaille
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// La longitude de l'entaille
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// La clé étrangère optionnelle de l'arbre entaillé
    /// </summary>
    public Guid? IdArbre { get; set; }

    /// <summary>
    /// La clé étrangère optionnelle de la ligne de tubelure raccordée
    /// </summary>
    public Guid? IdLigneTubelure { get; set; }
}
