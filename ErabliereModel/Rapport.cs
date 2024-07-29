using ErabliereApi.Donnees.Ownable;
using System;
using System.Collections.Generic;

namespace ErabliereApi.Donnees;

/// <summary>
/// Modèle d'un rapport
/// </summary>
public class Rapport : IIdentifiable<Guid?, Rapport>, IErabliereOwnable
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
    /// Objet JSON des paramètres de la requête pour générer le rapport
    /// </summary>
    public string RequestParameters { get; set; } = string.Empty;

    /// <summary>
    /// Date de création du rapport
    /// </summary>
    public DateTime DateCreation { get; set; }

    /// <summary>
    /// Données du rapport
    /// </summary>
    public List<RapportDonnees> Donnees { get; set; } = new List<RapportDonnees>();

    /// <summary>
    /// Compare les rapports par date de création
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public int CompareTo(Rapport? other)
    {
        if (other == null)
        {
            return 1;
        }

        return DateCreation.CompareTo(other.DateCreation);
    }
}
