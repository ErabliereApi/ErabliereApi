using System;
using System.ComponentModel.DataAnnotations;
using ErabliereApi.Donnees.Interfaces;

namespace ErabliereApi.Donnees.Action.Post
{
    /// <summary>
    /// Modèle d'ajout d'une érablière
    /// </summary>
    public class PostErabliere : ILocalizable
    {
        /// <summary>
        /// La clé primaire de l'érablière. Paramètre optionnel. Si absent
        /// un Id aléatoire sera généré.
        /// </summary>
        public Guid? Id { get; set; }

        /// <summary>
        /// Le nom de l'érablière
        /// </summary>
        [MaxLength(100, ErrorMessage = "Le nom de l'érablière ne peut pas dépasser 100 caractères.")]
        [Required(ErrorMessage = "Le nom de l'érablière ne peut pas être vide.")]
        public string? Nom { get; set; }

        /// <summary>
        /// Spécifie les ip qui peuvent créer des opérations d'alimentation pour cette érablière.
        /// </summary>
        [MaxLength(50, ErrorMessage = "L'adresse IP ne peut pas dépasser 50 caractères.")]
        public string? IpRules { get; set; }

        /// <summary>
        /// Un indice permettant l'affichage des érablières dans l'ordre précisé.
        /// </summary>
        public int? IndiceOrdre { get; set; }

        /// <summary>
        /// Code postal, utilisé pour les prédictions météo
        /// </summary>
        [MaxLength(30, ErrorMessage = "Le code postal ne peut pas dépasser 30 caractères.")]
        public string? CodePostal { get; set; }

        /// <summary>
        /// Indicateur permettant de déterminer si la section des barils sera utiliser par l'érablière
        /// </summary>
        public bool? AfficherSectionBaril { get; set; }

        /// <summary>
        /// Indicateur permettant de déterminer si la section des donnees sera utiliser par l'érablière
        /// </summary>
        public bool? AfficherTrioDonnees { get; set; }

        /// <summary>
        /// Indicateur permettant de déterminer si la section des donnees sera utiliser par l'érablière
        /// </summary>
        public bool? AfficherSectionDompeux { get; set; }

        /// <summary>
        /// Indiquateur permettant une accès en lecture à l'érablière sans authentification
        /// </summary>
        public bool IsPublic { get; set; }

        /// <inheritdoc />
        public double Latitude { get; set; }

        /// <inheritdoc />
        public double Longitude { get; set; }
    }
}
