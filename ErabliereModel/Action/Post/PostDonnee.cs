using ErabliereModel.Interfaces;
using System;
using System.ComponentModel.DataAnnotations;

namespace ErabliereApi.Donnees.Action.Post
{
    /// <summary>
    /// Modèle de création d'une entité <see cref="Donnee"/>
    /// </summary>
    public class PostDonnee : IDonneeTexte
    {
        /// <summary>
        /// Date de la transaction
        /// </summary>
        public DateTimeOffset? D { get; set; }

        /// <summary>
        /// La température en dixième de degré Celsius
        /// </summary>
        /// <example>250 = 25.0</example>
        public short? T { get; set; }

        /// <summary>
        /// Niveau bassin en pourcentage
        /// </summary>
        public short? NB { get; set; }

        /// <summary>
        /// Vacuum en dixième de HG
        /// </summary>
        /// <example>250 = 25.0</example>
        public short? V { get; set; }

        /// <summary>
        /// Id de dl'érablière relier a cette donnée
        /// </summary>
        [Required]
        public Guid? IdErabliere { get; set; }

        /// <inheritdoc />
        public string? GetDonneeTexte()
        {
            return $"Température: {(T.HasValue ? (T.Value / 10.0).ToString("0.0") + "°C" : "N/A")}, Vacuum: {(V.HasValue ? (V.Value / 10.0).ToString("0.0") + "HG" : "N/A")}, Niveau bassin: {(NB.HasValue ? NB.Value.ToString("0") + "%" : "N/A")}";
        }
    }
}
