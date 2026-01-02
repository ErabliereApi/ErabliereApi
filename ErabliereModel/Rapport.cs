using ErabliereApi.Donnees.Interfaces;
using ErabliereApi.Donnees.Ownable;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ErabliereApi.Donnees;

/// <summary>
/// Modèle d'un rapport
/// </summary>
public class Rapport : IIdentifiable<Guid?, Rapport>, IErabliereOwnable, IDatesInfo
{
    /// <summary>
    /// Clé primaire du rapport
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// L'id de l'érabliere
    /// </summary>
    public Guid? IdErabliere { get; set; }

    /// <inheritdoc />
    public Erabliere? Erabliere { get; set; }

    /// <summary>
    /// Le type du rapport
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Utilisation du trio de donnée au lieu des données d'un capteur
    /// </summary>
    public bool UtiliserTemperatureTrioDonnee { get; set; }

    /// <summary>
    /// Date de début
    /// </summary>
    public DateTime DateDebut { get; set; }

    /// <summary>
    /// Date de fin
    /// </summary>
    public DateTime DateFin { get; set; }

    /// <summary>
    /// Date de dernière modification du rapport
    /// </summary>
    public DateTimeOffset? DateModification { get; set; }

    /// <summary>
    /// Le seuil de température
    /// </summary>
    [Column(TypeName = "decimal(18, 2)")]
    public decimal SeuilTemperature { get; set; }

    /// <summary>
    /// Afficher le rapport dans la page principale
    /// </summary>
    public bool AfficherDansDashboard { get; set; }

    /// <summary>
    /// Objet JSON des paramètres de la requête pour générer le rapport
    /// </summary>
    public string RequestParameters { get; set; } = string.Empty;

    /// <summary>
    /// Le nom du rapport, soit temperature pour le trio de données ou le nom du capteur
    /// </summary>
    [MaxLength(50)]
    public string Nom { get; set; } = string.Empty;

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
    /// Données du rapport
    /// </summary>
    public List<RapportDonnees> Donnees { get; set; } = new List<RapportDonnees>();

    /// <inheritdoc />
    public DateTimeOffset? DC { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? DM { get; set; }

    /// <summary>
    /// Capteur associé au rapport si applicable
    /// </summary>
    public Capteur? Capteur { get; set; }

    /// <summary>
    /// Compare les rapports par date de création
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public int CompareTo(Rapport? other)
    {
        if (other?.DC == null)
        {
            return 1;
        }

        if (DC == null)
        {
            return -1;
        }

        return DC.Value.CompareTo(other.DC.Value);
    }
}
