using System;

namespace ErabliereApi.Donnees.Action.Put;

/// <summary>
/// Modèle de modification d'un dompeux. Ne contient volontairement aucune propriété de navigation
/// afin d'empêcher l'over-posting vers des entités liées.
/// </summary>
public class PutDompeux
{
    /// <summary>
    /// L'id du dompeux à modifier
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// La clé étrangère de l'érablière
    /// </summary>
    public Guid? IdErabliere { get; set; }

    /// <summary>
    /// Date de l'occurence
    /// </summary>
    public DateTimeOffset? T { get; set; }

    /// <summary>
    /// La date de début
    /// </summary>
    public DateTimeOffset? DD { get; set; }

    /// <summary>
    /// La date de fin
    /// </summary>
    public DateTimeOffset? DF { get; set; }
}
