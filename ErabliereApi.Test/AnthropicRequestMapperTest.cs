using Anthropic.Models.Messages;
using ErabliereApi.Services.AI;
using ErabliereApi.Services.AI.Tools;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace ErabliereApi.Test;

/// <summary>
/// La traduction de la conversation vers l'api de messages d'Anthropic.
///
/// C'est ce qui permet à ErabliereAI d'utiliser les outils MCP quand le fournisseur
/// configuré est Anthropic. Une erreur ici ne fait pas échouer l'appel : elle donne au
/// modèle une conversation subtilement fausse — un résultat d'outil qu'il croit avoir
/// écrit lui-même, ou une instruction système présentée comme sa propre parole — et
/// la réponse est alors fausse sans que rien ne le signale.
/// </summary>
public class AnthropicRequestMapperTest
{
    [Fact]
    public void LesMessagesSystemeDeviennentLeParametreSysteme()
    {
        List<ChatMessage> messages = [
            new SystemChatMessage("Vous êtes un acériculteur."),
            new UserChatMessage("Combien de barils?"),
            new SystemChatMessage("Répondez maintenant.")
        ];

        var system = AnthropicRequestMapper.MapSystem(messages);

        Assert.NotNull(system);
        Assert.Equal(
            ["Vous êtes un acériculteur.", "Répondez maintenant."],
            system.Select(bloc => bloc.Text));

        // Et surtout : pas répétés comme un tour de la conversation, où le message de
        // fin de boucle deviendrait une parole du modèle.
        var tours = AnthropicRequestMapper.MapMessages(messages);

        var tour = Assert.Single(tours);
        Assert.Equal(Role.User, (Role)tour.Role);
        Assert.Equal("Combien de barils?", TexteDu(Assert.Single(Blocs(tour))));
    }

    [Fact]
    public void SansMessageSystemeLeSystemeResteAbsent()
    {
        Assert.Null(AnthropicRequestMapper.MapSystem([new UserChatMessage("Bonjour")]));
    }

    [Fact]
    public void UnAppelDOutilDevientUnBlocToolUse()
    {
        List<ChatMessage> messages = [
            new UserChatMessage("Combien de barils?"),
            AppelOutil("appel-1", "get_barils", """{"idErabliere":"1"}""")
        ];

        var tours = AnthropicRequestMapper.MapMessages(messages);

        Assert.Equal(2, tours.Count);
        Assert.Equal(Role.Assistant, (Role)tours[1].Role);

        var bloc = Assert.Single(Blocs(tours[1]));

        Assert.True(bloc.TryPickToolUse(out ToolUseBlockParam? appel));
        Assert.Equal("get_barils", appel!.Name);
        Assert.Equal("appel-1", appel.ID);
        Assert.Equal("1", appel.Input!["idErabliere"].GetString());
    }

    [Fact]
    public void UnResultatDOutilDevientUnBlocToolResult()
    {
        List<ChatMessage> messages = [
            new UserChatMessage("Combien de barils?"),
            AppelOutil("appel-1", "get_barils", "{}"),
            new ToolChatMessage("appel-1", """{"total":42}""")
        ];

        var tours = AnthropicRequestMapper.MapMessages(messages);

        Assert.Equal(3, tours.Count);

        // Le rôle importe : c'est quelque chose qu'on donne au modèle, pas qu'il a dit.
        Assert.Equal(Role.User, (Role)tours[2].Role);

        var bloc = Assert.Single(Blocs(tours[2]));

        Assert.True(bloc.TryPickToolResult(out ToolResultBlockParam? resultat));
        // Anthropic apparie par id : celui de l'appel qui a produit ce résultat.
        Assert.Equal("appel-1", resultat!.ToolUseID);
        Assert.True(resultat.Content!.TryPickString(out var texte));
        Assert.Equal("""{"total":42}""", texte);
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

        var tours = AnthropicRequestMapper.MapMessages(messages);

        // Anthropic exige les résultats des appels parallèles dans un seul message
        // user, là où la boucle les remet un message à la fois.
        Assert.Equal(3, tours.Count);
        Assert.Equal(
            ["appel-1", "appel-2"],
            Blocs(tours[2]).Select(bloc =>
            {
                Assert.True(bloc.TryPickToolResult(out ToolResultBlockParam? resultat));
                return resultat!.ToolUseID;
            }));
    }

    [Fact]
    public void UnResultatSansAppelCorrespondantEstRemisEnTexte()
    {
        // Défensif : la boucle envoie toujours l'appel avant son résultat. Anthropic
        // refuse un tool_result sans tool_use correspondant, et le résultat serait perdu.
        var tours = AnthropicRequestMapper.MapMessages([new ToolChatMessage("inconnu", """{"total":42}""")]);

        var bloc = Assert.Single(Blocs(Assert.Single(tours)));

        Assert.False(bloc.TryPickToolResult(out _));
        Assert.Equal("""{"total":42}""", TexteDu(bloc));
    }

    [Fact]
    public void UnMessageVideNeDevientPasUnTour()
    {
        // Un message sans bloc est refusé par l'api.
        Assert.Empty(AnthropicRequestMapper.MapMessages([new AssistantChatMessage("")]));
    }

    [Fact]
    public void LIdDeLAppelEstRenvoyeTelQuel()
    {
        // Anthropic apparie par tool_use_id, comme OpenAI par id : les ids toolu_…
        // font l'aller-retour intacts, et aucun id synthétique n'existe ici —
        // contrairement à GeminiRequestMapper.
        var appel = Assert.Single(AnthropicRequestMapper.MapToolCalls(Reponse(
            BlocAppel("toolu_1", "get_barils"))));

        Assert.Equal("toolu_1", appel.Id);

        List<ChatMessage> suite = [
            AppelOutil(appel.Id, appel.FunctionName, appel.FunctionArguments),
            new ToolChatMessage(appel.Id, "{}")
        ];

        var tours = AnthropicRequestMapper.MapMessages(suite);

        Assert.True(Assert.Single(Blocs(tours[0])).TryPickToolUse(out ToolUseBlockParam? renvoye));
        Assert.Equal("toolu_1", renvoye!.ID);

        Assert.True(Assert.Single(Blocs(tours[1])).TryPickToolResult(out ToolResultBlockParam? resultat));
        Assert.Equal("toolu_1", resultat!.ToolUseID);
    }

    [Fact]
    public void UneReponseSansAppelDOutilNEnRapporteAucun()
    {
        Assert.Empty(AnthropicRequestMapper.MapToolCalls(Reponse(new TextBlock { Text = "Bonjour", Citations = null })));
    }

    [Fact]
    public void LesArgumentsDeLAppelSontResserialisesEnObjetJson()
    {
        var arguments = new Dictionary<string, JsonElement>
        {
            ["idErabliere"] = JsonSerializer.SerializeToElement("1"),
            ["top"] = JsonSerializer.SerializeToElement(5)
        };

        var appel = Assert.Single(AnthropicRequestMapper.MapToolCalls(Reponse(
            BlocAppel("toolu_1", "get_barils", arguments))));

        // La boucle et le jeu d'outils lisent les arguments comme un objet json.
        var relus = JsonDocument.Parse(appel.FunctionArguments).RootElement;

        Assert.Equal("1", relus.GetProperty("idErabliere").GetString());
        Assert.Equal(5, relus.GetProperty("top").GetInt32());
    }

    [Fact]
    public void LeTexteDeLaReponseIgnoreLesPensees()
    {
        var texte = AnthropicRequestMapper.MapText(Reponse(
            new ThinkingBlock { Thinking = "Je devrais consulter les barils.", Signature = "sig" },
            new TextBlock { Text = "Vous avez 42 barils.", Citations = null }));

        Assert.Equal("Vous avez 42 barils.", texte);
    }

    [Fact]
    public void LeTexteDUneReponseQuiNeFaitQuAppelerUnOutilEstNul()
    {
        Assert.Null(AnthropicRequestMapper.MapText(Reponse(
            BlocAppel("toolu_1", "get_barils"))));
    }

    [Fact]
    public void UneImageParUriDevientUnBlocImage()
    {
        List<ChatMessage> messages = [
            new UserChatMessage(
                ChatMessageContentPart.CreateTextPart("Que montre cette photo?"),
                ChatMessageContentPart.CreateImagePart(new Uri("https://exemple.test/photo.png")))
        ];

        var blocs = Blocs(Assert.Single(AnthropicRequestMapper.MapMessages(messages)));

        Assert.Equal(2, blocs.Count);
        Assert.True(blocs[1].TryPickImage(out ImageBlockParam? image));
        Assert.True(image!.Source.TryPickUrlImage(out UrlImageSource? source));
        Assert.Equal("https://exemple.test/photo.png", source!.Url);
    }

    [Fact]
    public void UneImageEnBase64DevientUnBlocImage()
    {
        var octets = new byte[] { 1, 2, 3, 4 };

        List<ChatMessage> messages = [
            new UserChatMessage(
                ChatMessageContentPart.CreateTextPart("Que montre cette photo?"),
                ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(octets), "image/png"))
        ];

        var blocs = Blocs(Assert.Single(AnthropicRequestMapper.MapMessages(messages)));

        Assert.Equal(2, blocs.Count);
        Assert.True(blocs[1].TryPickImage(out ImageBlockParam? image));
        Assert.True(image!.Source.TryPickBase64Image(out Base64ImageSource? source));
        Assert.Equal(Convert.ToBase64String(octets), source!.Data);
        Assert.Equal("image/png", (string)source.MediaType);
    }

    [Fact]
    public void LesSchemasDesOutilsDuCatalogueSontTraduisibles()
    {
        // Le garde-fou du branchement : les schémas viennent d'AIFunctionFactory, écrits
        // pour l'api d'OpenAI. S'ils cessaient d'être lisibles par Anthropic, chaque
        // conversation outillée échouerait sur ce fournisseur.
        var catalogue = new ErabliereAiToolCatalog(Options.Create(new ErabliereAiToolOptions()));

        var options = new ChatCompletionOptions();
        foreach (var outil in catalogue.ChatTools)
        {
            options.Tools.Add(outil);
        }

        var outils = AnthropicRequestMapper.MapTools(options);

        Assert.NotNull(outils);
        Assert.Equal(catalogue.ChatTools.Count, outils.Count);

        for (var i = 0; i < outils.Count; i++)
        {
            Assert.True(outils[i].TryPickTool(out Tool? outil));

            Assert.False(string.IsNullOrWhiteSpace(outil!.Name));
            Assert.False(string.IsNullOrWhiteSpace(outil.Description));

            // Chaque paramètre doit survivre à la traduction. Un schéma vidé ne casse
            // rien visiblement : le modèle appelle l'outil les mains vides et bâtit sa
            // réponse sur un échec dont personne ne l'a averti.
            var attendus = JsonDocument.Parse(catalogue.ChatTools[i].FunctionParameters!.ToString())
                                       .RootElement.GetProperty("properties")
                                       .EnumerateObject()
                                       .Select(propriete => propriete.Name);

            Assert.Equal(attendus, outil.InputSchema.Properties!.Keys);
        }
    }

    [Fact]
    public void UnParametreOptionnelGardeSonUnionDeTypes()
    {
        // Anthropic lit le json schema standard : l'union du générateur de schéma passe
        // telle quelle, là où GeminiRequestMapper doit la réécrire.
        var options = new ChatCompletionOptions();
        options.Tools.Add(ChatTool.CreateFunctionTool("get_barils", "Les barils.", BinaryData.FromString(
            """{"type":"object","properties":{"top":{"type":["integer","null"],"default":null}}}""")));

        Assert.True(Assert.Single(AnthropicRequestMapper.MapTools(options)!).TryPickTool(out Tool? outil));

        var top = outil!.InputSchema.Properties!["top"];

        Assert.Equal(
            ["integer", "null"],
            top.GetProperty("type").EnumerateArray().Select(type => type.GetString()));
    }

    [Fact]
    public void UnSchemaIntraduisibleEstSignale()
    {
        // Plutôt que de laisser partir une déclaration sans paramètres.
        var options = new ChatCompletionOptions();
        options.Tools.Add(ChatTool.CreateFunctionTool("get_barils", "Les barils.", BinaryData.FromString(
            """{"type":"array"}""")));

        var exception = Assert.Throws<InvalidOperationException>(() => AnthropicRequestMapper.MapTools(options));

        Assert.Contains("get_barils", exception.Message);
    }

    [Fact]
    public void SansOutilDeclareLaRequeteNEnPorteAucun()
    {
        Assert.Null(AnthropicRequestMapper.MapTools(new ChatCompletionOptions()));
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
    /// La réponse du fournisseur, réduite aux blocs de contenu : le reste des membres
    /// obligatoires du modèle du sdk n'intéresse pas le mappeur.
    /// </summary>
    private static Message Reponse(params ContentBlock[] blocs)
    {
        return new Message
        {
            ID = "msg_test",
            Container = null,
            Content = blocs,
            Model = "claude-opus-5",
            StopDetails = null,
            StopReason = null,
            StopSequence = null,
            Usage = new Usage
            {
                CacheCreation = null,
                CacheCreationInputTokens = null,
                CacheReadInputTokens = null,
                InferenceGeo = null,
                InputTokens = 0,
                OutputTokens = 0,
                OutputTokensDetails = null,
                ServerToolUse = null,
                ServiceTier = null
            }
        };
    }

    /// <summary>
    /// Un bloc tool_use tel que le fournisseur le rapporte.
    /// </summary>
    private static ToolUseBlock BlocAppel(string id, string nom, Dictionary<string, JsonElement>? arguments = null)
    {
        return new ToolUseBlock
        {
            ID = id,
            Name = nom,
            Input = arguments ?? [],
            Caller = new DirectCaller()
        };
    }

    private static IReadOnlyList<ContentBlockParam> Blocs(MessageParam tour)
    {
        Assert.True(tour.Content.TryPickContentBlockParams(out var blocs));

        return blocs!;
    }

    private static string? TexteDu(ContentBlockParam bloc)
    {
        Assert.True(bloc.TryPickText(out TextBlockParam? texte));

        return texte!.Text;
    }
}
