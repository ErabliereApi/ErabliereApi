﻿using System.ComponentModel.DataAnnotations;

namespace ErabliereApi.Models
{
    /// <summary>
    /// Modèle d'ajout d'une note
    /// </summary>
    public class PostNoteMultipart
    {
        /// <summary>
        /// Le titre de la note
        /// </summary>
        [MaxLength(200)]
        [Required]
        public string? Title { get; set; }

        /// <summary>
        /// Le text de la note
        /// </summary>
        [MaxLength(2000)]
        public string? Text { get; set; }

        /// <summary>
        /// Fichier obtenu depuis le multipart
        /// </summary>
        public IFormFile? File { get; set; }

        /// <summary>
        /// L'extension du fichier
        /// </summary>
        [MaxLength(20)]
        public string? FileExtension { get; set; }

        /// <summary>
        /// La date de cération
        /// </summary>
        public DateTimeOffset? Created { get; set; }

        /// <summary>
        /// La date de la note
        /// </summary>
        public DateTimeOffset? NoteDate { get; set; }
    }
}
