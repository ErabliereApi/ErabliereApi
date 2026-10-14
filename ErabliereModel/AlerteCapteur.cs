using ErabliereApi.Donnees.Interfaces;
using ErabliereApi.Donnees.Ownable;
using ErabliereModel.Interfaces;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ErabliereApi.Donnees;

/// <summary>
/// Une alerte d'un capteur
/// </summary>
public class AlerteCapteur : IIdentifiable<Guid?, AlerteCapteur>, ILevelTwoOwnable<Capteur>, IAlerteTexte
{
    /// <summary>
    /// La clé primaire
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// L'id du capteur
    /// </summary>
    public Guid? IdCapteur { get; set; }

    /// <summary>
    /// Le capteur de l'alerte
    /// </summary>
    public Capteur? Capteur { get; set; }

    /// <summary>
    /// Le nom de l'alerte
    /// </summary>
    [MaxLength(100)]
    public string? Nom { get; set; }

    /// <summary>
    /// Une liste d'adresse email séparer par des ';'
    /// </summary>
    /// <example>exemple@courriel.com;exemple2@courriel.com</example>
    [MaxLength(200)]
    public string? EnvoyerA { get; set; }

    /// <summary>
    /// Une liste de numéros de téléphone séparés par des ';'
    /// </summary>
    /// <example>+14375327599;+15749375019</example>
    [MaxLength(200)]
    public string? TexterA { get; set; }

    /// <summary>
    /// Date création
    /// </summary>
    public DateTime? DC { get; set; }

    /// <summary>
    /// Date de modification
    /// </summary>
    public DateTimeOffset? DM { get; set; }

    /// <summary>
    /// La valeur minimal de ce capteur
    /// </summary>
    [Column("MinValue")]
    public decimal? MinValue { get; set; }

    /// <summary>
    /// La valeur maximal de ce capteur
    /// </summary>
    public decimal? MaxValue { get; set; }

    /// <summary>
    /// Indique si l'alerte est activé
    /// </summary>
    public bool IsEnable { get; set; }

    /// <summary>
    /// Marge de retour (hystérésis). Une alerte commencée sous <see cref="MinValue"/> ne se termine
    /// qu'au-dessus de MinValue + Marge, et une alerte commencée au-dessus de <see cref="MaxValue"/>
    /// qu'en dessous de MaxValue - Marge. Null équivaut à 0.
    /// </summary>
    public decimal? Marge { get; set; }

    /// <summary>
    /// Indique si l'alerte est présentement en cours. Géré par le serveur.
    /// </summary>
    public bool EstEnAlerte { get; set; }

    /// <summary>
    /// Date de la donnée qui a causé le dernier changement d'état. Une donnée plus ancienne
    /// ne peut pas renverser l'état. Géré par le serveur.
    /// </summary>
    public DateTimeOffset? DateChangementEtat { get; set; }

    /// <summary>
    /// Date de la donnée qui a commencé la dernière alerte
    /// </summary>
    public DateTimeOffset? LastOccurence { get; set; }

    /// <inheritdoc />
    public Capteur? Owner { get => Capteur; set { Capteur = value; } }

    /// <inheritdoc />
    public Guid? OwnerId { get => IdCapteur; set { IdCapteur = value; } }

    /// <inheritdoc />
    public int CompareTo(AlerteCapteur? other)
    {
        return Id.HasValue ? Id.Value.CompareTo(other?.Id) : -1;
    }

    /// <inheritdoc />
    public string? GetAlerteTexte()
    {
        return string.IsNullOrWhiteSpace(Nom) ? IdCapteur?.ToString() : Nom;
    }
}
