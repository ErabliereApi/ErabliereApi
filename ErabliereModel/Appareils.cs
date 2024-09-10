using ErabliereApi.Donnees.Interfaces;
using ErabliereApi.Donnees.Ownable;
using System;
using System.Collections.Generic;

namespace ErabliereApi.Donnees;

/// <summary>
/// Appareil de l'érablière
/// </summary>
public class Appareil : IIdentifiable<Guid?, Appareil>, IErabliereOwnable, ILocalizable, IDatesInfo
{
    /// <summary>
    /// Id de l'appareil
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Nom de l'appareil
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Description de l'appareil
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Id de l'érablière
    /// </summary>
    public Guid? IdErabliere { get; set; }

    /// <summary>
    /// L'érablière
    /// </summary>
    public Erabliere? Erabliere { get; set; }

    /// <summary>
    /// Latitude
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// Longitude
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// Capteurs de l'appareil
    /// </summary>
    public List<Capteur>? Capteurs { get; set; }

    /// <summary>
    /// La date de création
    /// </summary>
    public DateTimeOffset? DC { get; set; }

    /// <inheritdoc />
    public int CompareTo(Appareil? other)
    {
        if (other == null)
        {
            return 1;
        }

        return string.Compare(Name, other.Name, StringComparison.Ordinal);
    }
}