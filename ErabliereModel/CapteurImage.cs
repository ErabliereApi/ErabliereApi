using ErabliereApi.Donnees.Interfaces;
using ErabliereApi.Donnees.Ownable;
using System;
using System.ComponentModel.DataAnnotations;

namespace ErabliereApi.Donnees;

/// <summary>
/// Représente un capteur d'image
/// </summary>
public class CapteurImage : IIdentifiable<Guid?, CapteurImage>, IErabliereOwnable, ILocalizable, IDatesInfo
{
    /// <summary>
    /// L'id du capteur
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Id de l'érablière lié à ce capteur
    /// </summary>
    public Guid? IdErabliere { get; set; }

    /// <summary>
    /// L'érablière liée à ce capteur
    /// </summary>
    public Erabliere? Erabliere { get; set; }


    /// <summary>
    /// Le nom donné au capteur
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string? Nom { get; set; }

    /// <summary>
    /// L'url du capteur (au protocol rtsp)
    /// </summary>
    /// <example>
    /// rtsp://url-de-votre-capteur.com
    /// </example>
    [Required]
    [MaxLength(200)]
    [RegularExpression("/^rtsp:\\/\\/[-a-zA-Z0-9@:%._+~#=]{1,256}\\.[a-zA-Z0-9()]{1,6}\\b([-a-zA-Z0-9()@:%_+.~#?&/=]*)$/i", ErrorMessage = "L'url doit être une url au protocol rtsp valide")]
    public string? Url { get; set; }

    /// <summary>
    /// Le port du capteur.
    /// </summary>
    [Required]
    [MaxLength(5)]
    public string? Port { get; set; }

    /// <summary>
    /// L'identifiant utilisé pour se connecter au flux d'image.
    /// </summary>
    [MaxLength(200)]
    public string? Identifiant { get; set; }

    /// <summary>
    /// Le mot de passe utilisé pour se connecter au flux d'image.
    /// </summary>
    [MaxLength(200)]
    public string? MotDePasse { get; set; }

    /// <summary>
    /// La position d'affichage du capteur.
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "La valeur doit être plus grande que 0")]
    public int Ordre { get; set; }

    /// <inheritdoc />
    public double Latitude { get; set; }

    /// <inheritdoc />
    public double Longitude { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? DC { get; set; }

    /// <inheritdoc />
    public int CompareTo(CapteurImage? other)
    {
        if (other == null)
        {
            return 1;
        }

        return Ordre.CompareTo(other.Ordre);
    }
}
