using System;

namespace ErabliereApi.Donnees.Action.Post
{
    /// <summary>
    /// Requête pour le rapport de degré jour
    /// </summary>
    public class PostRapportDegreeJourRequest
    {
        /// <summary>
        /// Identifiant de l'érablière
        /// </summary>
        public Guid IdErabliere { get; set; }

        /// <summary>
        /// Identifiant du capteur
        /// </summary>
        public Guid? IdCapteur { get; set; }

        /// <summary>
        /// Utilisation du trio de donnée au lieu des données d'un capteur
        /// </summary>
        public bool UtiliserTemperatureTrioDonnee { get; set; }

        /// <summary>
        /// Date de début
        /// </summary>
        public DateTime DateDebut { get; set; }

        /// <summary>
        /// Date de fin
        /// </summary>
        public DateTime DateFin { get; set; }

        /// <summary>
        /// Le seuil de température
        /// </summary>
        public decimal SeuilTemperature { get; set; }
    }
}