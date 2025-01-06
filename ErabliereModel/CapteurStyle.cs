using System;
using ErabliereApi.Donnees.Interfaces;

namespace ErabliereApi.Donnees;

/// <summary>
/// Le style d'un capteur
/// </summary>
public class CapteurStyle : IIdentifiable<Guid?, CapteurStyle>
{
    /// <summary>
    /// La clé primaire
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// La couleur de fond
    /// </summary>
    public string? BackgroundColor { get; set; }

    /// <summary>
    /// La couleur
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// La couleur de la bordure
    /// </summary>
    public string? BorderColor { get; set; }

    /// <summary>
    /// Remplir sous la courbe
    /// </summary>
    public string? Fill { get; set; }

    /// <summary>
    /// La couleur des points
    /// </summary>
    public string? PointBackgroundColor { get; set; }

    /// <summary>
    /// La couleur de la bordure des points
    /// </summary>
    public string? PointBorderColor { get; set; }

    /// <summary>
    /// La tension entre les points de la courbe
    /// </summary>
    public double? Tension { get; set; }

    /// <summary>
    /// La couleur de la bordure de la courbe
    /// </summary>
    public string? DSetBorderColor { get; set; }

    /// <summary>
    /// Utilier un gradient
    /// </summary>
    public bool UseGradient { get; set; }

    /// <summary>
    /// Point d'arrêt 1 du gradient
    /// </summary>
    public double? G1Stop { get; set; }

    /// <summary>
    /// Point d'arrêt 2 du gradient
    /// </summary>
    public double? G2Stop { get; set; }

    /// <summary>
    /// Point d'arrêt 3 du gradient
    /// </summary>
    public double? G3Stop { get; set; }

    /// <summary>
    /// Couleur 1 du gradient
    /// </summary>
    public string? G1Color { get; set; }

    /// <summary>
    /// Couleur 2 du gradient
    /// </summary>
    public string? G2Color { get; set; }

    /// <summary>
    /// Couleur 3 du gradient
    /// </summary>
    public string? G3Color { get; set; }

    /// <summary>
    /// L'id du capteur
    /// </summary>
    public Guid? IdCapteur { get; set; }

    /// <summary>
    /// Le capteur
    /// </summary>
    public Capteur? Capteur { get; set; }

    /// <inheritdoc />
    public int CompareTo(CapteurStyle? other)
    {
        return 0;
    }
}