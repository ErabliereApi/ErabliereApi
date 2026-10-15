using System;

namespace ErabliereApi.Donnees.Action.Get;

/// <summary>
/// Un kit d'une érablière, avec la dernière utilisation de sa clé d'API
/// </summary>
public class GetKit
{
    /// <summary>
    /// L'id du kit
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Le nom du kit
    /// </summary>
    public string? Nom { get; set; }

    /// <summary>
    /// Le type du kit
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// L'id du capteur d'alimentation secteur
    /// </summary>
    public Guid? IdCapteurAlimentation { get; set; }

    /// <summary>
    /// L'id du capteur de charge de la batterie
    /// </summary>
    public Guid? IdCapteurCharge { get; set; }

    /// <summary>
    /// L'id du capteur d'autonomie de la batterie
    /// </summary>
    public Guid? IdCapteurAutonomie { get; set; }

    /// <summary>
    /// La date de création du kit
    /// </summary>
    public DateTimeOffset? DC { get; set; }

    /// <summary>
    /// La date de révocation du kit, null s'il est actif
    /// </summary>
    public DateTimeOffset? DateRevocation { get; set; }

    /// <summary>
    /// La dernière utilisation de la clé d'API du kit, à cinq minutes près
    /// </summary>
    public DateTimeOffset? DerniereUtilisation { get; set; }
}
