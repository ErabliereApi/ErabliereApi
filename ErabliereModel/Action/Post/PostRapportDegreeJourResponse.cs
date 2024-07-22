using System.Collections.Generic;

namespace ErabliereApi.Donnees.Action.Post;

/// <summary>
/// Résultat du rapport de degré jour
/// </summary>
public class PostRapportDegreeJourResponse 
{
    /// <summary>
    /// La requête
    /// </summary>
    public PostRapportDegreeJourRequest Requete { get; set; } = new PostRapportDegreeJourRequest();

    /// <summary>
    /// Les données du rapport
    /// </summary>
    public List<RapportDegreeJour> Rapport { get; set; } = new List<RapportDegreeJour>();

    /// <summary>
    /// Les données du rapport
    /// </summary>
    public class RapportDegreeJour
    {
        /// <summary>
        /// La date
        /// </summary>
        public string Date { get; set; } = string.Empty;

        /// <summary>
        /// La température
        /// </summary>
        public decimal Temperature { get; set; }

        /// <summary>
        /// Le degré jour
        /// </summary>
        public decimal DegreJour { get; set; }
    }
}