using System;
using System.ComponentModel.DataAnnotations;

namespace ErabliereApi.Donnees.Action.Put;

/// <summary>
/// Modèle pour la mise à jour d'un rapport
/// </summary>
public class PutRapport
{
    /// <summary>
    /// Le nom du rapport, soit temperature pour le trio de données ou le nom du capteur
    /// </summary>
    [MaxLength(50)]
    public string? Nom { get; set; } = string.Empty;

    /// <summary>
    /// Le type du rapport
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Utilisation du trio de donnée au lieu des données d'un capteur
    /// </summary>
    public bool? UtiliserTemperatureTrioDonnee { get; set; }

    /// <summary>
    /// Date de début
    /// </summary>
    public DateTime? DateDebut { get; set; }

    /// <summary>
    /// Date de fin
    /// </summary>
    public DateTime? DateFin { get; set; }

    /// <summary>
    /// Le seuil de température
    /// </summary>
    public decimal? SeuilTemperature { get; set; }

    /// <summary>
    /// Afficher le rapport dans la page principale
    /// </summary>
    public bool? AfficherDansDashboard { get; set; }
}