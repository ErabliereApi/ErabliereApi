using System.ComponentModel;
using ErabliereAPI.Proxy;
using ErabliereApi.Mcp.Models;
using ModelContextProtocol.Server;

namespace ErabliereApi.Mcp.Tools;

/// <summary>
/// Read-only tools exposing the notes of a maple grove.
/// </summary>
[McpServerToolType]
public static class NoteTools
{
    /// <summary>
    /// The fields read from the API.
    /// </summary>
    /// <remarks>
    /// Without an explicit $select the endpoint returns the attached file inline,
    /// base 64 encoded. One photograph would blow past the response budget on its
    /// own, so the projection happens server side rather than in
    /// <see cref="NoteSummary"/>.
    /// </remarks>
    private const string SelectedFields = "id,idErabliere,title,text,noteDate,created,fileName,fileExtension,fileSize,isPublic";

    /// <summary>
    /// Lists the notes of a maple grove, most recent first.
    /// </summary>
    [McpServerTool(Name = "get_notes", ReadOnly = true, Idempotent = true)]
    [Description("Lists the notes of a maple grove, most recent first: the producer's journal of observations, interventions and incidents, with the metadata of any attached file. " +
                 "Use this to answer what happened on a given date, to find an intervention by keyword, or to give context to an anomaly seen in the sensor data. " +
                 "The body of a note is cut at 400 characters, and attached files are never returned. Returns an envelope {summary, data, truncated}.")]
    public static async Task<ToolResponse<IReadOnlyList<NoteSummary>>> GetNotesAsync(
        IErabliereAPIProxy proxy,
        [Description("Identifier (GUID) of the maple grove, as returned by list_erablieres.")]
        string erabliereId,
        [Description("Optional case-sensitive substring searched in both the title and the body of the notes. Omit to list them all.")]
        string? search = null,
        [Description("Maximum number of notes to return, between 1 and 100. Defaults to 25.")]
        int? top = null,
        CancellationToken cancellationToken = default)
    {
        var id = ToolArguments.ParseId(erabliereId, nameof(erabliereId));
        var validatedTop = ToolArguments.ValidateTop(top);
        var filter = ToolArguments.BuildContainsAnyFilter(search, "title", "text");

        var notes = await proxy.NotesAllAsync(
            id,
            select: SelectedFields,
            filter: filter,
            top: validatedTop,
            skip: null,
            count: null,
            expand: null,
            orderby: "noteDate desc",
            cancellationToken);

        var summaries = notes.Select(NoteSummary.From).ToArray();
        var truncated = summaries.Length == validatedTop;

        var summary = summaries.Length == 0
            ? "No note matching the query."
            : $"{summaries.Length} notes, most recent first, the latest being '{summaries[0].Title}'.";

        return ToolResponse.ForList(summary, summaries, truncated);
    }
}
