using ErabliereApi.Donnees.Interfaces;
using ErabliereApi.Donnees.Ownable;
using System;
using System.ComponentModel.DataAnnotations;

namespace ErabliereApi.Donnees;

/// <summary>
/// Un kit matériel rattaché à une érablière, par exemple un Raspberry Pi avec une batterie de
/// secours qui détecte les pannes de courant. Le kit envoie ses données avec sa propre clé
/// d'API, restreinte aux routes de ses capteurs.
/// </summary>
/// <remarks>
/// Les capteurs et la clé sont référencés par leur id sans clé étrangère : retirer le kit ne
/// supprime ni les capteurs ni leur historique, qui appartiennent à l'érablière.
/// </remarks>
public class Kit : IIdentifiable<Guid?, Kit>, IErabliereOwnable, IDatesInfo
{
    /// <summary>
    /// Type d'un kit de détection de panne de courant (script monitorUpsV2.py)
    /// </summary>
    public const string TypePanne = "panne";

    /// <summary>
    /// La clé primaire
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// L'id de l'érablière où le kit est installé
    /// </summary>
    public Guid? IdErabliere { get; set; }

    /// <summary>
    /// L'érablière où le kit est installé
    /// </summary>
    public Erabliere? Erabliere { get; set; }

    /// <summary>
    /// Le nom du kit
    /// </summary>
    [MaxLength(50)]
    public string? Nom { get; set; }

    /// <summary>
    /// Le type de kit, voir <see cref="TypePanne" />
    /// </summary>
    [MaxLength(50)]
    public string? Type { get; set; }

    /// <summary>
    /// L'id de la clé d'API du kit
    /// </summary>
    public Guid? IdApiKey { get; set; }

    /// <summary>
    /// L'id du capteur d'alimentation secteur (1 alimenté, 0 en panne)
    /// </summary>
    public Guid? IdCapteurAlimentation { get; set; }

    /// <summary>
    /// L'id du capteur de charge de la batterie, en pourcentage
    /// </summary>
    public Guid? IdCapteurCharge { get; set; }

    /// <summary>
    /// L'id du capteur d'autonomie de la batterie, en minutes
    /// </summary>
    public Guid? IdCapteurAutonomie { get; set; }

    /// <summary>
    /// La date de révocation du kit. Sa clé d'API est révoquée à la même date.
    /// </summary>
    public DateTimeOffset? DateRevocation { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? DC { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? DM { get; set; }

    /// <inheritdoc />
    public int CompareTo(Kit? other)
    {
        return Nullable.Compare(DC, other?.DC);
    }
}
