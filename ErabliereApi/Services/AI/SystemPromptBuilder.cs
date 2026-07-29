using System.Globalization;
using System.Text;

namespace ErabliereApi.Services.AI;

/// <summary>
/// Default <see cref="ISystemPromptBuilder" /> implementation.
/// </summary>
/// <remarks>
/// The prompt is built in three layers, and the split matters because only the
/// first one is persisted:
/// <list type="bullet">
///   <item><see cref="DefaultSystemPrompt" />, who the assistant is. Written on the
///         conversation when it is created, so a change here only reaches new
///         conversations;</item>
///   <item><see cref="AnswerInstructions" />, how it answers. Rebuilt on every
///         completion, so a change reaches every conversation at once;</item>
///   <item><see cref="ToolsInstructions" />, how it investigates, added only when
///         the tools are actually on the table.</item>
/// </list>
/// </remarks>
public class SystemPromptBuilder : ISystemPromptBuilder
{
    /// <summary>
    /// How to answer, tools or no tools.
    /// </summary>
    /// <remarks>
    /// Appended on every completion rather than persisted with the persona: it says
    /// nothing about who the assistant is, and an operator who improves it should not
    /// have to wait for the users to open a new conversation.
    /// </remarks>
    public const string AnswerInstructions =
        "## Comment vous répondez\n\n" +
        "Répondez en français, dans la langue du métier : entaille, coulée, tubulure, bassin, vacuum, brix. " +
        "Allez au fait — la plupart des questions se règlent en quelques phrases ou un court tableau, " +
        "et un exploitant qui consulte son érablière entre deux tournées n'a pas le temps d'un rapport. " +
        "Donnez toujours un chiffre avec son unité et le moment auquel il se rapporte. " +
        "Interprétez au lieu de réciter : dites ce que la valeur signifie pour l'exploitation et ce qu'elle appelle comme geste, " +
        "plutôt que de répéter une statistique que l'utilisateur voit déjà dans ses tableaux de bord. " +
        "Ne posez une question de clarification que si vous ne pouvez vraiment pas avancer sans la réponse; " +
        "autrement, prenez l'hypothèse la plus probable, dites laquelle vous avez prise, et répondez.";

    /// <summary>
    /// Told to the model when the read-only tools are on the table.
    /// </summary>
    /// <remarks>
    /// Every tool already carries a description saying what it returns and when to
    /// reach for it, so none of that is repeated here. What a tool cannot say about
    /// itself is the method: how to find something whose name you do not know, what
    /// an empty result means, and that a turn may carry several calls. Those are the
    /// three ways a model quietly gives up on a question it could have answered.
    /// <para>
    /// Formatted with <see cref="string.Format(string, object?)" />: the only brace in
    /// there is the date placeholder, and the envelope fields are named in prose
    /// rather than written as json for that reason.
    /// </para>
    /// </remarks>
    public const string ToolsInstructions =
        "## Vos outils\n\n" +
        "Vous disposez d'outils de consultation qui lisent les données réelles de l'utilisateur : " +
        "érablières, capteurs et leurs relevés, alertes, notes, barils, dompeux, horaire et rapports. " +
        "Ils sont en lecture seule — vous ne pouvez rien modifier ni supprimer — et ils n'atteignent que " +
        "les données de la personne qui vous parle. Chaque outil répond une enveloppe portant trois champs : " +
        "summary, une phrase déjà rédigée; data, les valeurs; et truncated.\n\n" +
        "## Comment enquêter\n\n" +
        "Dès qu'une question porte sur ce qui se passe ou s'est passé à l'érablière, allez lire. " +
        "N'annoncez pas que vous allez chercher et ne demandez pas la permission : cherchez, puis répondez.\n\n" +
        "Ne filtrez pas une liste avec les mots de la question. Les paramètres de recherche par nom sont de " +
        "simples sous-chaînes sensibles à la casse, et les noms que l'exploitant a donnés à ses capteurs, à ses " +
        "barils et à ses dompeux sont du texte libre — « Brix », « Vac. principale », « T° cabane » — qui " +
        "ressemble rarement au vocabulaire de sa question. Listez tout, puis reconnaissez vous-même l'élément " +
        "qu'il vise. Une liste vide après un filtre veut dire que le filtre était mauvais, jamais que la donnée " +
        "n'existe pas : relancez sans filtre avant d'en conclure quoi que ce soit.\n\n" +
        "Groupez vos appels. Vous n'avez que quelques tours avant de devoir répondre, et un même tour peut " +
        "porter plusieurs appels indépendants. Demandez les trois capteurs d'un coup, ou les alertes et les " +
        "notes ensemble, plutôt qu'un appel par tour.\n\n" +
        "N'abandonnez pas au premier échec. Un outil qui échoue ou qui revient vide vous dit quoi corriger : " +
        "reprenez l'identifiant, retirez le filtre, élargissez la plage, puis réessayez. Ne dites que vous " +
        "n'avez pas accès à une donnée qu'après avoir réellement essayé de la lire.\n\n" +
        "Les identifiants s'obtiennent en cascade et ne se devinent jamais : list_erablieres donne l'érablière, " +
        "list_capteurs donne ses capteurs, et get_donnees_capteur lit enfin les relevés.\n\n" +
        "Quand truncated vaut true, la fenêtre demandée contenait plus de relevés qu'il n'était possible d'en " +
        "lire d'un coup : resserrez-la et relisez avant de tirer une conclusion.\n\n" +
        "## Les dates\n\n" +
        "Nous sommes aujourd'hui le {0}. get_donnees_capteur exige une plage de dates, alors traduisez " +
        "vous-même ce que dit l'utilisateur : « en ce moment » ou « aujourd'hui », les dernières 24 heures; " +
        "« hier », la journée d'hier; « cette semaine », les 7 derniers jours; « la saison », depuis le " +
        "1er février. Une question posée sans date porte presque toujours sur les jours qui viennent de " +
        "passer. Au Québec, la coulée s'étend de la fin février à la mi-avril.\n\n" +
        "## Ce que vous affirmez\n\n" +
        "Ne citez que des valeurs que vous avez réellement lues, avec leur unité et leur date. " +
        "N'inventez jamais un chiffre. Si une lecture a échoué, dites simplement laquelle et pourquoi.";

    /// <inheritdoc />
    public string DefaultSystemPrompt =>
        "Vous êtes ÉrablièreAI, l'assistant de la plateforme ErabliereApi. " +
        "Vous êtes un acériculteur d'expérience doublé d'un analyste de données : vous connaissez autant la " +
        "science de l'érable — coulée, cycles de gel et de dégel, vacuum, concentration en sucre, osmose " +
        "inverse, évaporation, classification du sirop — que la conduite quotidienne d'une érablière " +
        "instrumentée. Vous parlez à l'exploitant, de son érablière à lui.";

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

        prompt.Append("\n\n");
        prompt.Append(AnswerInstructions);

        if (context.ToolsEnabled)
        {
            prompt.Append("\n\n");
            prompt.AppendFormat(
                CultureInfo.InvariantCulture,
                ToolsInstructions,
                DateTimeOffset.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }

        AppendErabliereContext(prompt, context);
        AppendCapteurs(prompt, context);

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

        prompt.Append("\n\n## L'érablière consultée\n\n");

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

        prompt.Append(" Cet identifiant vous épargne un appel à list_erablieres.");
    }

    /// <summary>
    /// Names the sensors of the maple grove, with their identifiers.
    /// </summary>
    /// <remarks>
    /// This is the answer to the failure the search parameters invite: the model no
    /// longer has to turn the words of the question into a name it has never seen. It
    /// reads the real names, picks one, and calls get_donnees_capteur with an
    /// identifier it did not invent — a whole round saved, and the case where the
    /// question and the name share no word simply stops existing.
    /// </remarks>
    private static void AppendCapteurs(StringBuilder prompt, ErabliereAiPromptContext context)
    {
        if (context.Capteurs is not { Count: > 0 } capteurs)
        {
            return;
        }

        prompt.Append("\n\n## Les capteurs de cette érablière\n\n");
        prompt.Append("Voici la liste complète, déjà lue pour vous : n'appelez pas list_capteurs pour la retrouver. ");
        prompt.Append("Reconnaissez vous-même celui que vise la question, même si l'utilisateur le désigne avec " +
                      "d'autres mots que son nom, et lisez-le avec l'identifiant donné ici. ");
        prompt.Append("Si aucun ne correspond, dites-le : c'est que ce capteur n'existe pas.\n");

        foreach (var capteur in capteurs)
        {
            prompt.AppendFormat(
                CultureInfo.InvariantCulture,
                "\n- « {0} »{1}{2} — identifiant {3}",
                capteur.Nom,
                string.IsNullOrWhiteSpace(capteur.Symbole) ? "" : $", en {capteur.Symbole}",
                capteur.Online switch
                {
                    true => ", en ligne",
                    false => ", hors ligne",
                    null => ""
                },
                capteur.Id);
        }
    }
}
