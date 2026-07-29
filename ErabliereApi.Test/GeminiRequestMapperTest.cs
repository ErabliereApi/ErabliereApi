using ErabliereApi.Services.AI;
using ErabliereApi.Services.AI.Tools;
using Google.GenAI.Types;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace ErabliereApi.Test;

/// <summary>
/// La traduction de la conversation vers l'api de Gemini.
///
/// C'est ce qui permet à ErabliereAI d'utiliser les outils MCP quand le fournisseur
/// configuré est Google. Une erreur ici ne fait pas échouer l'appel : elle donne au
/// modèle une conversation subtilement fausse — un résultat d'outil qu'il croit avoir
/// écrit lui-même, ou une instruction système présentée comme sa propre parole — et
/// la réponse est alors fausse sans que rien ne le signale.
/// </summary>
public class GeminiRequestMapperTest
{
    [Fact]
    public void LesMessagesSystemeDeviennentLInstructionSysteme()
    {
        List<ChatMessage> messages = [
            new SystemChatMessage("Vous êtes un acériculteur."),
            new UserChatMessage("Combien de barils?"),
            new SystemChatMessage("Répondez maintenant.")
        ];

        var instruction = GeminiRequestMapper.MapSystemInstruction(messages);

        Assert.NotNull(instruction);
        Assert.Equal(
            ["Vous êtes un acériculteur.", "Répondez maintenant."],
            instruction.Parts!.Select(p => p.Text));

        // Et surtout : pas répétés comme un tour de la conversation, où le message de
        // fin de boucle deviendrait une parole du modèle.
        var contents = GeminiRequestMapper.MapContents(messages);

        var contenu = Assert.Single(contents);
        Assert.Equal(GeminiRequestMapper.UserRole, contenu.Role);
        Assert.Equal("Combien de barils?", Assert.Single(contenu.Parts!).Text);
    }

    [Fact]
    public void SansMessageSystemeLInstructionResteAbsente()
    {
        Assert.Null(GeminiRequestMapper.MapSystemInstruction([new UserChatMessage("Bonjour")]));
    }

    [Fact]
    public void UnAppelDOutilDevientUnePartieFunctionCall()
    {
        List<ChatMessage> messages = [
            new UserChatMessage("Combien de barils?"),
            AppelOutil("appel-1", "get_barils", """{"idErabliere":"1"}""")
        ];

        var contents = GeminiRequestMapper.MapContents(messages);

        Assert.Equal(2, contents.Count);
        Assert.Equal(GeminiRequestMapper.ModelRole, contents[1].Role);

        var appel = Assert.Single(contents[1].Parts!).FunctionCall;

        Assert.NotNull(appel);
        Assert.Equal("get_barils", appel.Name);
        Assert.Equal("appel-1", appel.Id);
        Assert.Equal("1", Assert.IsType<JsonElement>(appel.Args!["idErabliere"]).GetString());
    }

    [Fact]
    public void UnResultatDOutilDevientUneFunctionResponseNommee()
    {
        List<ChatMessage> messages = [
            new UserChatMessage("Combien de barils?"),
            AppelOutil("appel-1", "get_barils", "{}"),
            new ToolChatMessage("appel-1", """{"total":42}""")
        ];

        var contents = GeminiRequestMapper.MapContents(messages);

        Assert.Equal(3, contents.Count);

        // Le rôle importe : c'est quelque chose qu'on donne au modèle, pas qu'il a dit.
        Assert.Equal(GeminiRequestMapper.UserRole, contents[2].Role);

        var resultat = Assert.Single(contents[2].Parts!).FunctionResponse;

        Assert.NotNull(resultat);
        // Gemini apparie par nom : celui de l'appel qui a produit ce résultat.
        Assert.Equal("get_barils", resultat.Name);
        Assert.Equal(42, Assert.IsType<JsonElement>(resultat.Response!["total"]).GetInt32());
    }

    [Fact]
    public void LesResultatsDUnMemeTourSontRegroupes()
    {
        List<ChatMessage> messages = [
            new UserChatMessage("Compare mes deux érablières."),
            AppelOutil([("appel-1", "get_erabliere", "{}"), ("appel-2", "get_barils", "{}")]),
            new ToolChatMessage("appel-1", """{"nom":"Ferme"}"""),
            new ToolChatMessage("appel-2", """{"total":42}""")
        ];

        var contents = GeminiRequestMapper.MapContents(messages);

        Assert.Equal(3, contents.Count);
        Assert.Equal(
            ["get_erabliere", "get_barils"],
            contents[2].Parts!.Select(p => p.FunctionResponse!.Name));
    }

    [Fact]
    public void UnResultatQuiNEstPasUnObjetEstEnveloppe()
    {
        List<ChatMessage> messages = [
            AppelOutil("appel-1", "list_erablieres", "{}"),
            new ToolChatMessage("appel-1", "[]")
        ];

        var resultat = Assert.Single(GeminiRequestMapper.MapContents(messages)[1].Parts!).FunctionResponse;

        // Gemini type une réponse de fonction comme un objet, là où OpenAI accepte
        // n'importe quel texte.
        Assert.Equal(
            JsonValueKind.Array,
            Assert.IsType<JsonElement>(resultat!.Response![GeminiRequestMapper.ResultKey]).ValueKind);
    }

    [Fact]
    public void UnResultatSansAppelCorrespondantEstRemisEnTexte()
    {
        // Défensif : la boucle envoie toujours l'appel avant son résultat. Sans nom,
        // une réponse de fonction serait refusée par l'api, et le résultat perdu.
        var contents = GeminiRequestMapper.MapContents([new ToolChatMessage("inconnu", """{"total":42}""")]);

        var partie = Assert.Single(Assert.Single(contents).Parts!);

        Assert.Null(partie.FunctionResponse);
        Assert.Equal("""{"total":42}""", partie.Text);
    }

    [Fact]
    public void UnMessageVideNeDevientPasUnTour()
    {
        // Un contenu sans partie est refusé par l'api.
        Assert.Empty(GeminiRequestMapper.MapContents([new AssistantChatMessage("")]));
    }

    [Fact]
    public void UnIdDAppelDonneParGeminiLuiEstRenvoye()
    {
        var appels = GeminiRequestMapper.MapToolCalls(Reponse(
            new Part { FunctionCall = new FunctionCall { Id = "fc-1", Name = "get_barils" } }));

        var appel = Assert.Single(appels);
        Assert.Equal("fc-1", appel.Id);

        var resultat = PremierResultat(appel);
        Assert.Equal("fc-1", resultat.Id);
        Assert.Equal("get_barils", resultat.Name);
    }

    [Fact]
    public void UnIdSynthetiseNEstJamaisRenvoyeAGemini()
    {
        // Hors de l'api live, Gemini ne nomme pas d'id, mais la boucle en exige un :
        // ToolChatMessage est écrit sur le modèle d'OpenAI. Celui qu'on invente ne doit
        // pas ressortir comme s'il venait de Gemini.
        var appel = Assert.Single(GeminiRequestMapper.MapToolCalls(Reponse(
            new Part { FunctionCall = new FunctionCall { Name = "get_barils" } })));

        Assert.StartsWith(GeminiRequestMapper.SyntheticToolCallIdPrefix, appel.Id);

        var resultat = PremierResultat(appel);
        Assert.Null(resultat.Id);
        // L'appariement tient quand même : il se fait par le nom.
        Assert.Equal("get_barils", resultat.Name);
    }

    [Fact]
    public void DeuxAppelsSansIdRestentDistincts()
    {
        var appels = GeminiRequestMapper.MapToolCalls(Reponse(
            new Part { FunctionCall = new FunctionCall { Name = "get_barils" } },
            new Part { FunctionCall = new FunctionCall { Name = "get_barils" } }));

        Assert.Equal(2, appels.Count);
        Assert.NotEqual(appels[0].Id, appels[1].Id);
    }

    [Fact]
    public void UneReponseSansAppelDOutilNEnRapporteAucun()
    {
        Assert.Empty(GeminiRequestMapper.MapToolCalls(Reponse(new Part { Text = "Bonjour" })));
    }

    [Fact]
    public void LeTexteDeLaReponseIgnoreLesPensees()
    {
        var texte = GeminiRequestMapper.MapText(Reponse(
            new Part { Text = "Je devrais consulter les barils.", Thought = true },
            new Part { Text = "Vous avez 42 barils." }));

        Assert.Equal("Vous avez 42 barils.", texte);
    }

    [Fact]
    public void LeTexteDUneReponseQuiNeFaitQuAppelerUnOutilEstNul()
    {
        Assert.Null(GeminiRequestMapper.MapText(Reponse(
            new Part { FunctionCall = new FunctionCall { Name = "get_barils" } })));
    }

    [Fact]
    public void LesSchemasDesOutilsDuCatalogueSontTraduisibles()
    {
        // Le garde-fou du branchement : les schémas viennent d'AIFunctionFactory, écrits
        // pour l'api d'OpenAI. S'ils cessaient d'être lisibles par Gemini, chaque
        // conversation outillée échouerait sur ce fournisseur.
        var catalogue = new ErabliereAiToolCatalog(Options.Create(new ErabliereAiToolOptions()));

        var options = new ChatCompletionOptions();
        foreach (var outil in catalogue.ChatTools)
        {
            options.Tools.Add(outil);
        }

        var declarations = Assert.Single(GeminiRequestMapper.MapTools(options)!).FunctionDeclarations;

        Assert.NotNull(declarations);
        Assert.Equal(catalogue.ChatTools.Count, declarations.Count);

        for (var i = 0; i < declarations.Count; i++)
        {
            var declaration = declarations[i];

            Assert.False(string.IsNullOrWhiteSpace(declaration.Name));
            Assert.False(string.IsNullOrWhiteSpace(declaration.Description));
            Assert.NotNull(declaration.Parameters);
            Assert.Equal(Google.GenAI.Types.Type.Object, declaration.Parameters.Type);

            // Chaque paramètre doit survivre à la traduction. Un schéma vidé ne casse
            // rien visiblement : le modèle appelle l'outil les mains vides et bâtit sa
            // réponse sur un échec dont personne ne l'a averti.
            var attendus = JsonDocument.Parse(catalogue.ChatTools[i].FunctionParameters!.ToString())
                                       .RootElement.GetProperty("properties")
                                       .EnumerateObject()
                                       .Select(propriete => propriete.Name);

            Assert.Equal(attendus, declaration.Parameters.Properties!.Keys);
        }
    }

    [Fact]
    public void UnParametreOptionnelDevientUnTypeNullable()
    {
        // Le générateur de schéma type un paramètre optionnel avec une union, que
        // Schema.FromJson refuse — en rendant null pour le schéma entier, sans rien dire.
        var options = new ChatCompletionOptions();
        options.Tools.Add(ChatTool.CreateFunctionTool("get_barils", "Les barils.", BinaryData.FromString(
            """{"type":"object","properties":{"top":{"type":["integer","null"],"default":null}}}""")));

        var parametres = Assert.Single(GeminiRequestMapper.MapTools(options)!).FunctionDeclarations![0].Parameters;

        var top = Assert.Contains("top", parametres!.Properties!);
        Assert.Equal(Google.GenAI.Types.Type.Integer, top.Type);
        Assert.True(top.Nullable);
    }

    [Fact]
    public void UnSchemaIntraduisibleEstSignale()
    {
        // Plutôt que de laisser partir une déclaration sans paramètres.
        var options = new ChatCompletionOptions();
        options.Tools.Add(ChatTool.CreateFunctionTool("get_barils", "Les barils.", BinaryData.FromString(
            """{"type":"object","properties":{"top":{"type":{"impossible":true}}}}""")));

        var exception = Assert.Throws<InvalidOperationException>(() => GeminiRequestMapper.MapTools(options));

        Assert.Contains("get_barils", exception.Message);
    }

    [Fact]
    public void SansOutilDeclareLaRequeteNEnPorteAucun()
    {
        Assert.Null(GeminiRequestMapper.MapTools(new ChatCompletionOptions()));
    }

    /// <summary>
    /// Le message d'appel d'outil tel que la boucle le construit.
    /// </summary>
    private static AssistantChatMessage AppelOutil(string id, string nom, string arguments)
    {
        return AppelOutil([(id, nom, arguments)]);
    }

    private static AssistantChatMessage AppelOutil((string Id, string Nom, string Arguments)[] appels)
    {
        return new AssistantChatMessage(appels.Select(appel => ChatToolCall.CreateFunctionToolCall(
            appel.Id, appel.Nom, BinaryData.FromString(appel.Arguments))));
    }

    /// <summary>
    /// Le tour suivant tel que la boucle le construit, pour un appel rapporté par
    /// <see cref="GeminiRequestMapper.MapToolCalls" />, et la réponse de fonction qui
    /// en ressort.
    /// </summary>
    private static FunctionResponse PremierResultat(AIToolCall appel)
    {
        List<ChatMessage> messages = [
            AppelOutil(appel.Id, appel.FunctionName, "{}"),
            new ToolChatMessage(appel.Id, "{}")
        ];

        var resultat = Assert.Single(GeminiRequestMapper.MapContents(messages)[1].Parts!).FunctionResponse;

        Assert.NotNull(resultat);

        return resultat;
    }

    private static GenerateContentResponse Reponse(params Part[] parts)
    {
        return new GenerateContentResponse
        {
            Candidates = [new Candidate { Content = new Content { Role = "model", Parts = [.. parts] } }]
        };
    }
}
