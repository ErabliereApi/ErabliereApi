using ErabliereApi.Donnees.Interfaces;
using ErabliereApi.Donnees.Ownable;
using System;
using System.ComponentModel.DataAnnotations;

namespace ErabliereApi.Donnees;

/// <summary>
/// Une entaille cartographiée dans une érablière. Peut optionnellement être rattachée
/// à un arbre et/ou à une ligne de tubelure.
/// </summary>
public class Entaille : IIdentifiable<Guid?, Entaille>, IErabliereOwnable, ILocalizable, IDatesInfo
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
    /// L'érablière qui possède l'entaille
    /// </summary>
    public Erabliere? Erabliere { get; set; }

    /// <summary>
    /// L'identifiant ou le nom de l'entaille
    /// </summary>
    [MaxLength(100, ErrorMessage = "Le nom de l'entaille ne peut pas dépasser 100 caractères.")]
    public string? Nom { get; set; }

    /// <inheritdoc />
    public double Latitude { get; set; }

    /// <inheritdoc />
    public double Longitude { get; set; }

    /// <summary>
    /// La clé étrangère optionnelle de l'arbre entaillé
    /// </summary>
    public Guid? IdArbre { get; set; }

    /// <summary>
    /// L'arbre entaillé
    /// </summary>
    public Arbre? Arbre { get; set; }

    /// <summary>
    /// La clé étrangère optionnelle de la ligne de tubelure raccordée
    /// </summary>
    public Guid? IdLigneTubelure { get; set; }

    /// <summary>
    /// La ligne de tubelure raccordée
    /// </summary>
    public LigneTubelure? LigneTubelure { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? DC { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? DM { get; set; }

    /// <inheritdoc />
    public int CompareTo(Entaille? other)
    {
        return 0;
    }
}
