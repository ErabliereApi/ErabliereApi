namespace ErabliereApi.Services.AI.Tools;

/// <summary>
/// The sentence the chat shows while a tool runs.
/// </summary>
/// <remarks>
/// The labels are in French because they are read by the user, unlike the tool
/// descriptions, which are read by the model and stay in English with the rest of
/// the MCP tool set.
/// </remarks>
public static class ToolActivityLabels
{
    /// <summary>
    /// Shown while the model decides what to do.
    /// </summary>
    public const string Thinking = "ErabliereAI réfléchit…";

    /// <summary>
    /// Shown for a tool that has no label of its own.
    /// </summary>
    public const string DefaultToolLabel = "Consultation des données de l'érablière…";

    private static readonly Dictionary<string, string> Labels = new(StringComparer.Ordinal)
    {
        ["list_erablieres"] = "Consultation de vos érablières…",
        ["get_erabliere"] = "Consultation de l'érablière…",
        ["list_capteurs"] = "Consultation des capteurs…",
        ["get_donnees_capteur"] = "Consultation des données de capteurs…",
        ["get_alertes"] = "Consultation des alertes…",
        ["get_alertes_capteur"] = "Consultation des alertes de capteur…",
        ["get_notes"] = "Consultation des notes…",
        ["get_barils"] = "Consultation des barils…",
        ["get_dompeux"] = "Consultation des dompeux…",
        ["get_horaire"] = "Consultation de l'horaire…",
        ["list_rapports"] = "Consultation des rapports…",
        ["get_rapport"] = "Consultation d'un rapport…"
    };

    /// <summary>
    /// The sentence to show for a tool.
    /// </summary>
    public static string For(string? toolName)
    {
        if (string.IsNullOrWhiteSpace(toolName))
        {
            return DefaultToolLabel;
        }

        return Labels.GetValueOrDefault(toolName, DefaultToolLabel);
    }
}
