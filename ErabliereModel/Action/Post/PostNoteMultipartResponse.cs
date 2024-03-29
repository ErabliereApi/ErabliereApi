using System;
/// <summary>
/// Modèle d'une réponse d'un POST Note utilisant multipart/form-data
/// </summary>
public class PostNoteMultipartResponse 
{
        /// <summary>
    /// La clé primaire
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// La clé étrangère de l'érablière
    /// </summary>
    public Guid? IdErabliere { get; set; }

    /// <summary>
    /// Le titre de la note
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Le text de la note
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// L'extension du fichier
    /// </summary>
    public string? FileExtension { get; set; }

    /// <summary>
    /// Date de cération de la note
    /// </summary>
    public DateTimeOffset? Created { get; set; }

    /// <summary>
    /// Date de la note
    /// </summary>
    public DateTimeOffset? NoteDate { get; set; }
}