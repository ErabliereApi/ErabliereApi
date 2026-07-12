using ErabliereApi.Donnees.Interfaces;
using ErabliereApi.Donnees.Ownable;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ErabliereApi.Donnees;

/// <summary>
/// Un arbre cartographié dans une érablière
/// </summary>
public class Arbre : IIdentifiable<Guid?, Arbre>, IErabliereOwnable, ILocalizable, IDatesInfo
{
    /// <summary>
    /// La clé primaire
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// La clé étrangère de l'érablière
    /// </summary>
    public Guid? IdErabliere { get; set; }

    /// <summary>
    /// L'érablière qui possède l'arbre
    /// </summary>
    public Erabliere? Erabliere { get; set; }

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

    /// <inheritdoc />
    public double Latitude { get; set; }

    /// <inheritdoc />
    public double Longitude { get; set; }

    /// <summary>
    /// Les entailles de l'arbre
    /// </summary>
    public List<Entaille>? Entailles { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? DC { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? DM { get; set; }

    /// <inheritdoc />
    public int CompareTo(Arbre? other)
    {
        return 0;
    }
}
