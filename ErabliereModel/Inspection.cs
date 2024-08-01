using ErabliereApi.Donnees.Interfaces;
using ErabliereApi.Donnees.Ownable;
using System;
using System.Collections.Generic;

namespace ErabliereApi.Donnees;

/// <summary>
/// Modèle représentant une inspection
/// </summary>
public class Inspection : IIdentifiable<Guid?, Inspection>, IErabliereOwnable
{
    /// <summary>
    /// La clé primaire de l'inspection
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// L'id de l'érablière à laquelle l'inspection est associée
    /// </summary>
    public Guid? IdErabliere { get; set; }

    /// <inheritdoc />
    public Erabliere? Erabliere { get; set; }

    /// <summary>
    /// La date de l'inspection
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Le titre de l'inspection
    /// </summary>
    public string Titre { get; set; } = string.Empty;

    /// <summary>
    /// La description de l'inspection
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Les données d'inspection associées à l'inspection
    /// </summary>
    public List<InspectionDonnees> Donnees { get; set; } = new List<InspectionDonnees>();

    /// <summary>
    /// Compare les inspections par date
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public int CompareTo(Inspection? other)
    {
        if (other == null)
        {
            return 1;
        }

        return Date.CompareTo(other.Date);
    }
}
