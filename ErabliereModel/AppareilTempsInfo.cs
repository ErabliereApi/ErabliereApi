using System;

namespace ErabliereApi.Donnees;

/// <summary>
/// Informations temporelles d'un appareil
/// </summary>
public class AppareilTempsInfo
{
    /// <summary>
    /// Identifiant unique de l'information temporelle
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Date de début de l'information temporelle
    /// </summary>
    public long DateDebut { get; set; }

    /// <summary>
    /// Date de fin de l'information temporelle
    /// </summary>
    public long DateFin { get; set; }

    /// <summary>
    /// Temps de réponse du capteur
    /// </summary>
    public long Strtt { get; set; }

    /// <summary>
    /// Variation du temps de réponse du capteur
    /// </summary>
    public long Rttvar { get; set; }

    /// <summary>
    /// Temps d'occupation du capteur
    /// </summary>
    public long To { get; set; }

    /// <summary>
    /// Identifiant de l'appareil
    /// </summary>
    public Guid IdAppareil { get; set; }
}