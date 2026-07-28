using System.Globalization;
using System.Text;

namespace ErabliereApi.Services.AI;

/// <summary>
/// Default <see cref="ISystemPromptBuilder" /> implementation.
/// </summary>
public class SystemPromptBuilder : ISystemPromptBuilder
{
    /// <summary>
    /// Told to the model when the read-only tools are on the table.
    /// </summary>
    /// <remarks>
    /// Short on purpose. Every tool already carries a description saying what it
    /// returns and when to reach for it; repeating that here would only spend
    /// context. What the tools cannot say themselves is the house rules: read the
    /// data instead of guessing it, and never claim a number that was not read.
    /// </remarks>
    public const string ToolsInstructions =
        "Vous avez accès à des outils de consultation qui lisent les données réelles de l'utilisateur : " +
        "érablières, capteurs et leurs relevés, alertes, notes, barils, dompeux, horaire et rapports. " +
        "Servez-vous en dès qu'une question porte sur ce qui s'est réellement passé à l'érablière, " +
        "plutôt que de répondre de mémoire ou d'inventer une valeur. " +
        "Ces outils sont en lecture seule : vous ne pouvez rien modifier ni supprimer. " +
        "Commencez par obtenir les identifiants dont vous avez besoin (list_erablieres, puis list_capteurs) " +
        "avant d'appeler un outil qui en demande un. " +
        "Si un outil échoue ou revient vide, dites-le simplement à l'utilisateur; n'inventez jamais de données. " +
        "Aujourd'hui nous sommes le {0}. " +
        "Répondez en français, et citez les valeurs que vous avez réellement lues (unité et date incluses).";

    /// <inheritdoc />
    public string DefaultSystemPrompt => "Vous êtes un acériculteur expérimenté avec des connaissance scientifique et pratique.";

    /// <inheritdoc />
    public string BuildForNewConversation(string? requestedSystemMessage)
    {
        return string.IsNullOrWhiteSpace(requestedSystemMessage) ?
            DefaultSystemPrompt :
            requestedSystemMessage;
    }

    /// <inheritdoc />
    public string BuildForCompletion(string? conversationSystemMessage)
    {
        return BuildForCompletion(conversationSystemMessage, ErabliereAiPromptContext.None);
    }

    /// <inheritdoc />
    public string BuildForCompletion(string? conversationSystemMessage, ErabliereAiPromptContext context)
    {
        var prompt = new StringBuilder(string.IsNullOrWhiteSpace(conversationSystemMessage) ?
            DefaultSystemPrompt :
            conversationSystemMessage);

        if (context.ToolsEnabled)
        {
            prompt.Append("\n\n");
            prompt.AppendFormat(
                CultureInfo.InvariantCulture,
                ToolsInstructions,
                DateTimeOffset.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }

        AppendErabliereContext(prompt, context);

        return prompt.ToString();
    }

    /// <summary>
    /// Names the maple grove the chat was opened from, so the model does not have to
    /// guess which one "mon érablière" means, nor spend a tool call finding out.
    /// </summary>
    private static void AppendErabliereContext(StringBuilder prompt, ErabliereAiPromptContext context)
    {
        if (context.ErabliereId is null || context.ErabliereId == Guid.Empty)
        {
            return;
        }

        prompt.Append("\n\n");

        if (string.IsNullOrWhiteSpace(context.ErabliereNom))
        {
            prompt.AppendFormat(
                CultureInfo.InvariantCulture,
                "L'utilisateur consulte présentement l'érablière dont l'identifiant est {0}. Utilisez cet identifiant lorsqu'il parle de « mon érablière » sans en nommer une autre.",
                context.ErabliereId);
        }
        else
        {
            prompt.AppendFormat(
                CultureInfo.InvariantCulture,
                "L'utilisateur consulte présentement l'érablière « {0} », dont l'identifiant est {1}. Utilisez cet identifiant lorsqu'il parle de « mon érablière » sans en nommer une autre.",
                context.ErabliereNom.Trim(),
                context.ErabliereId);
        }
    }
}
