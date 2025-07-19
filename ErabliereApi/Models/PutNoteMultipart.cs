using System.ComponentModel.DataAnnotations;

namespace ErabliereApi.Models
{
    /// <summary>
    /// Modèle d'ajout d'une note
    /// </summary>
    public class PutNoteMultipart
    {
        /// <summary>
        /// Fichier obtenu depuis le multipart
        /// </summary>
        public IFormFile? File { get; set; }

        /// <summary>
        /// L'extension du fichier
        /// </summary>
        [MaxLength(20)]
        public string? FileExtension { get; set; }
    }
}
