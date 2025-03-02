using ErabliereApi.Donnees.Interfaces;
using ErabliereApi.Donnees.Ownable;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ErabliereApi.Donnees;

/// <summary>
/// Modèle de données d'un rapport
/// </summary>
public class RapportDonnees : IIdentifiable<Guid?, RapportDonnees>, ILevelTwoOwnable<Rapport>
{
    /// <summary>
    /// Clé primaire de la donnée du rapport
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Id du rapport
    /// </summary>
    public Guid? OwnerId { get; set; }

    /// <summary>
    /// Instance du rapport
    /// </summary>
    public Rapport? Owner { get; set; }

    /// <summary>
    /// La date de la donnée
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// La moyenne de la donnée
    /// </summary>
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Moyenne { get; set; }

    /// <summary>
    /// La somme de la donnée
    /// </summary>
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Somme { get; set; }

    /// <summary>
    /// La valeur minimale de la donnée
    /// </summary>
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Min { get; set; }

    /// <summary>
    /// La valeur maximale de la donnée
    /// </summary>
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Max { get; set; }

    /// <summary>
    /// Compare les données de rapport par date
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public int CompareTo(RapportDonnees? other)
    {
        if (other == null)
        {
            return 1;
        }

        return Date.CompareTo(other.Date);
    }
}
