using ErabliereApi.Donnees.Interfaces;
using ErabliereApi.Donnees.Ownable;
using System;

namespace ErabliereApi.Donnees;

/// <summary>
/// Une entré de données dans une inspection
/// </summary>
public class InspectionDonnees : IIdentifiable<Guid?, InspectionDonnees>, ILevelTwoOwnable<Inspection>
{
    /// <summary>
    /// La clé primaire de l'entrée de données
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// L'id de l'inspection à laquelle l'entrée de données est associée
    /// </summary>
    public Guid? OwnerId { get; set; }

    /// <summary>
    /// Instance de l'inspection à laquelle l'entrée de données est associée
    /// </summary>
    public Inspection? Owner { get; set; }

    /// <summary>
    /// Titre de la données d'inspection
    /// </summary>
    public string Titre { get; set; } = string.Empty;

    /// <summary>
    /// Description de la données d'inspection
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// L'url de l'image si enregistrer dans un sockage externe
    /// </summary>
    public string ImgUrl { get; set; } = string.Empty;

    /// <summary>
    /// Les données de l'image si enregistrer dans la base de données
    /// </summary>
    public byte[]? ImgBytes { get; set; }

    /// <summary>
    /// Compare les entrées de données par titre
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public int CompareTo(InspectionDonnees? other)
    {
        if (other == null)
        {
            return 1;
        }

        return Titre.CompareTo(other.Titre);
    }
}
