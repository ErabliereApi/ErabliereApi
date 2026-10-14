using ErabliereApi.Donnees.Interfaces;
using ErabliereApi.Donnees.Ownable;
using System;
using System.ComponentModel.DataAnnotations;

namespace ErabliereApi.Donnees;

/// <summary>
/// Un épisode d'alerte d'un capteur, par exemple une panne de courant : du début de la condition
/// jusqu'à son retour à la normale. Créé et fermé par le serveur à chaque changement d'état
/// de l'<see cref="AlerteCapteur"/>.
/// </summary>
public class EpisodeAlerteCapteur : IIdentifiable<Guid?, EpisodeAlerteCapteur>, ILevelTwoOwnable<Capteur>
{
    /// <summary>
    /// La clé primaire
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// L'id de l'alerte qui a ouvert l'épisode. Null si l'alerte a été supprimée depuis :
    /// l'historique est conservé.
    /// </summary>
    public Guid? IdAlerteCapteur { get; set; }

    /// <summary>
    /// L'alerte qui a ouvert l'épisode
    /// </summary>
    public AlerteCapteur? AlerteCapteur { get; set; }

    /// <summary>
    /// L'id du capteur
    /// </summary>
    public Guid? IdCapteur { get; set; }

    /// <summary>
    /// Le capteur de l'épisode
    /// </summary>
    public Capteur? Capteur { get; set; }

    /// <summary>
    /// Le nom de l'alerte au moment de l'ouverture, conservé si l'alerte est supprimée
    /// </summary>
    [MaxLength(100)]
    public string? NomAlerte { get; set; }

    /// <summary>
    /// Date de la donnée qui a ouvert l'épisode
    /// </summary>
    public DateTimeOffset DateDebut { get; set; }

    /// <summary>
    /// Valeur de la donnée qui a ouvert l'épisode. Null pour un épisode repris d'une alerte
    /// déjà en cours lors de la création de l'historique.
    /// </summary>
    public decimal? ValeurDebut { get; set; }

    /// <summary>
    /// Date de la donnée qui a fermé l'épisode. Null tant que l'épisode est en cours.
    /// </summary>
    public DateTimeOffset? DateFin { get; set; }

    /// <summary>
    /// Valeur de la donnée qui a fermé l'épisode. Null si l'épisode est en cours
    /// ou si sa fin n'a pas été mesurée (voir <see cref="RaisonFin"/>).
    /// </summary>
    public decimal? ValeurFin { get; set; }

    /// <summary>
    /// La raison de la fin de l'épisode. Null tant que l'épisode est en cours.
    /// </summary>
    public RaisonFinEpisode? RaisonFin { get; set; }

    /// <summary>
    /// Ce que l'épisode mesure : une valeur hors des seuils, ou un capteur muet.
    /// Un épisode de silence n'a pas de valeur, ses dates sont des heures de réception.
    /// </summary>
    public TypeEpisodeAlerte Type { get; set; }

    /// <inheritdoc />
    public Capteur? Owner { get => Capteur; set { Capteur = value; } }

    /// <inheritdoc />
    public Guid? OwnerId { get => IdCapteur; set { IdCapteur = value; } }

    /// <inheritdoc />
    public int CompareTo(EpisodeAlerteCapteur? other)
    {
        return Id.HasValue ? Id.Value.CompareTo(other?.Id) : -1;
    }
}
