using ErabliereApi.Donnees.Interfaces;
using ErabliereApi.Donnees.Ownable;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ErabliereApi.Donnees;

/// <summary>
/// Une ligne du réseau de tubelure d'une érablière
/// </summary>
public class LigneTubelure : IIdentifiable<Guid?, LigneTubelure>, IErabliereOwnable, IDatesInfo
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
    /// L'érablière qui possède la ligne
    /// </summary>
    public Erabliere? Erabliere { get; set; }

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

    /// <summary>
    /// Les entailles raccordées à cette ligne
    /// </summary>
    public List<Entaille>? Entailles { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? DC { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? DM { get; set; }

    /// <inheritdoc />
    public int CompareTo(LigneTubelure? other)
    {
        return 0;
    }
}
