using System.Text.Json.Serialization;
using ErabliereAPI.Proxy;

namespace ErabliereApi.Mcp.Models;

/// <summary>
/// Projection of a note returned to the MCP client.
/// </summary>
/// <remarks>
/// The proxy DTO carries the attached file as a byte array. It must never reach
/// the model: a single photograph serialized in base 64 is worth more tokens than
/// this server is allowed to return in a whole response. Only the metadata of the
/// attachment is exposed.
/// </remarks>
/// <param name="Id">Identifier of the note.</param>
/// <param name="IdErabliere">Identifier of the maple grove owning the note.</param>
/// <param name="Title">Title of the note.</param>
/// <param name="Text">Body of the note, cut at <see cref="MaxTextLength"/> characters.</param>
/// <param name="NoteDate">Date the note is about, chosen by the producer.</param>
/// <param name="Created">Date the note was recorded.</param>
/// <param name="FileName">Name of the attached file, null when there is none.</param>
/// <param name="FileExtension">Extension of the attached file, null when there is none.</param>
/// <param name="FileSize">Size of the attached file in bytes, null when there is none.</param>
/// <param name="IsPublic">True when the note is publicly readable.</param>
public record NoteSummary(
    [property: JsonPropertyName("id")] Guid? Id,
    [property: JsonPropertyName("idErabliere")] Guid? IdErabliere,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("text")] string? Text,
    [property: JsonPropertyName("noteDate")] DateTimeOffset? NoteDate,
    [property: JsonPropertyName("created")] DateTimeOffset? Created,
    [property: JsonPropertyName("fileName")] string? FileName,
    [property: JsonPropertyName("fileExtension")] string? FileExtension,
    [property: JsonPropertyName("fileSize")] int? FileSize,
    [property: JsonPropertyName("isPublic")] bool? IsPublic)
{
    /// <summary>
    /// Number of characters of the body kept in a listing. The column allows two
    /// thousand, and twenty five of those would fill the whole response budget.
    /// </summary>
    public const int MaxTextLength = 400;

    /// <summary>
    /// Maps a proxy DTO to the projection exposed by the MCP tools.
    /// </summary>
    public static NoteSummary From(Note note)
    {
        ArgumentNullException.ThrowIfNull(note);

        return new NoteSummary(
            note.Id,
            note.IdErabliere,
            note.Title,
            Truncate(note.Text),
            note.NoteDate,
            note.Created,
            note.FileName,
            note.FileExtension,
            note.FileSize,
            note.IsPublic);
    }

    private static string? Truncate(string? text)
    {
        if (text is null || text.Length <= MaxTextLength)
        {
            return text;
        }

        return string.Concat(text.AsSpan(0, MaxTextLength), "… (truncated)");
    }
}
