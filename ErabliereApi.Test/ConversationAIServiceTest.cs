using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Donnees.Action.Post;
using ErabliereApi.Donnees.Contantes;
using ErabliereApi.Services.AI;
using ErabliereApi.Services.AI.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.ClientModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ErabliereApi.Test;

/// <summary>
/// Locks the behavior extracted from ErabliereAIController into ConversationAIService.
/// </summary>
public class ConversationAIServiceTest
{
    private const string DefaultSystemPrompt = "Vous êtes un acériculteur expérimenté avec des connaissance scientifique et pratique.";

    [Fact]
    public async Task SendPromptAsync_SansConversation_CreeLaConversationAvecLeUserId()
    {
        var (service, context, aiService) = CreateService();
        aiService.CompleteChatAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatCompletionOptions>(), Arg.Any<CancellationToken>())
                 .Returns(new AIResponse { Text = "Bonjour" });

        var prompt = new PostPrompt { Prompt = "Quand entailler?", PromptType = "Chat" };

        var response = await service.SendPromptAsync(prompt, "utilisateur@erabliere.ca", CancellationToken.None);

        var conversation = Assert.Single(context.Conversations);
        Assert.Equal("utilisateur@erabliere.ca", conversation.UserId);
        Assert.Equal("Quand entailler?", conversation.Name);
        Assert.Equal(DefaultSystemPrompt, conversation.SystemMessage);
        Assert.Equal(conversation.Id, prompt.ConversationId);
        Assert.Same(conversation, response.Conversation);
    }

    [Fact]
    public async Task SendPromptAsync_AvecPhraseSysteme_UtiliseCellePassee()
    {
        var (service, context, aiService) = CreateService();
        aiService.CompleteChatAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatCompletionOptions>(), Arg.Any<CancellationToken>())
                 .Returns(new AIResponse { Text = "Bonjour" });

        var prompt = new PostPrompt
        {
            Prompt = "Quand entailler?",
            PromptType = "Chat",
            SystemMessage = "Vous êtes un botaniste."
        };

        await service.SendPromptAsync(prompt, "utilisateur@erabliere.ca", CancellationToken.None);

        Assert.Equal("Vous êtes un botaniste.", Assert.Single(context.Conversations).SystemMessage);
    }

    [Fact]
    public async Task SendPromptAsync_ConversationExistante_EnvoieLHistoriqueEtLaPhraseSysteme()
    {
        var (service, context, aiService) = CreateService();
        var conversationId = Guid.NewGuid();
        context.Conversations.Add(new Conversation
        {
            Id = conversationId,
            UserId = "utilisateur@erabliere.ca",
            SystemMessage = "Vous êtes un botaniste.",
            CreatedOn = DateTimeOffset.Now,
            LastMessageDate = DateTimeOffset.Now
        });
        context.Messages.Add(new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Content = "Bonjour",
            IsUser = true,
            CreatedAt = DateTimeOffset.Now.AddMinutes(-2)
        });
        context.Messages.Add(new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Content = "Bonjour, comment puis-je aider?",
            IsUser = false,
            CreatedAt = DateTimeOffset.Now.AddMinutes(-1)
        });
        await context.SaveChangesAsync();

        List<ChatMessage>? envoyes = null;
        aiService.CompleteChatAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatCompletionOptions>(), Arg.Any<CancellationToken>())
                 .Returns(callInfo =>
                 {
                     envoyes = [.. callInfo.Arg<IEnumerable<ChatMessage>>()];
                     return Task.FromResult<AIResponse?>(new AIResponse { Text = "Vers la fin février." });
                 });

        var prompt = new PostPrompt
        {
            Prompt = "Quand entailler?",
            PromptType = "Chat",
            ConversationId = conversationId
        };

        await service.SendPromptAsync(prompt, "utilisateur@erabliere.ca", CancellationToken.None);

        Assert.NotNull(envoyes);
        Assert.Equal(4, envoyes.Count);
        Assert.IsType<SystemChatMessage>(envoyes[0]);
        Assert.Equal("Vous êtes un botaniste.", envoyes[0].Content[0].Text);
        Assert.IsType<UserChatMessage>(envoyes[1]);
        Assert.IsType<AssistantChatMessage>(envoyes[2]);
        Assert.IsType<UserChatMessage>(envoyes[3]);
        Assert.Equal("Quand entailler?", envoyes[3].Content[0].Text);
    }

    [Fact]
    public async Task SendPromptAsync_ConversationSansPhraseSysteme_UtiliseLaPhraseParDefaut()
    {
        var (service, context, aiService) = CreateService();
        var conversationId = Guid.NewGuid();
        context.Conversations.Add(new Conversation
        {
            Id = conversationId,
            UserId = "utilisateur@erabliere.ca",
            SystemMessage = "   "
        });
        await context.SaveChangesAsync();

        List<ChatMessage>? envoyes = null;
        aiService.CompleteChatAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatCompletionOptions>(), Arg.Any<CancellationToken>())
                 .Returns(callInfo =>
                 {
                     envoyes = [.. callInfo.Arg<IEnumerable<ChatMessage>>()];
                     return Task.FromResult<AIResponse?>(new AIResponse { Text = "Réponse" });
                 });

        await service.SendPromptAsync(
            new PostPrompt { Prompt = "Question", PromptType = "Chat", ConversationId = conversationId },
            "utilisateur@erabliere.ca",
            CancellationToken.None);

        Assert.NotNull(envoyes);
        Assert.Equal(DefaultSystemPrompt, envoyes[0].Content[0].Text);
    }

    [Fact]
    public async Task SendPromptAsync_PersisteLaQuestionEtLaReponse()
    {
        var (service, context, aiService) = CreateService();
        aiService.CompleteChatAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatCompletionOptions>(), Arg.Any<CancellationToken>())
                 .Returns(new AIResponse { Text = "Vers la fin février.", Refusal = null });

        var prompt = new PostPrompt { Prompt = "Quand entailler?", PromptType = "Chat" };

        var response = await service.SendPromptAsync(prompt, "utilisateur@erabliere.ca", CancellationToken.None);

        var messages = context.Messages.OrderBy(m => m.IsUser ? 0 : 1).ToList();
        Assert.Equal(2, messages.Count);
        Assert.Equal("Quand entailler?", messages[0].Content);
        Assert.True(messages[0].IsUser);
        Assert.Equal("Vers la fin février.", messages[1].Content);
        Assert.False(messages[1].IsUser);
        Assert.All(messages, m => Assert.Equal(prompt.ConversationId, m.ConversationId));
        Assert.Equal("Vers la fin février.", response.Response?.Content);
    }

    [Fact]
    public async Task SendPromptAsync_SansReponseDeLIA_PersisteAucuneReponse()
    {
        var (service, _, aiService) = CreateService();
        aiService.CompleteChatAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatCompletionOptions>(), Arg.Any<CancellationToken>())
                 .Returns((AIResponse?)null);

        var response = await service.SendPromptAsync(
            new PostPrompt { Prompt = "Question", PromptType = "Chat" },
            "utilisateur@erabliere.ca",
            CancellationToken.None);

        Assert.Equal("Aucune réponse", response.Response?.Content);
    }

    [Fact]
    public async Task SendPromptAsync_TypeCompletion_NEnvoiePasDHistorique()
    {
        var (service, context, aiService) = CreateService();
        var conversationId = Guid.NewGuid();
        context.Conversations.Add(new Conversation { Id = conversationId, UserId = "utilisateur@erabliere.ca" });
        context.Messages.Add(new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Content = "Bonjour",
            IsUser = true,
            CreatedAt = DateTimeOffset.Now
        });
        await context.SaveChangesAsync();

        List<ChatMessage>? envoyes = null;
        aiService.CompleteChatAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatCompletionOptions>(), Arg.Any<CancellationToken>())
                 .Returns(callInfo =>
                 {
                     envoyes = [.. callInfo.Arg<IEnumerable<ChatMessage>>()];
                     return Task.FromResult<AIResponse?>(new AIResponse { Text = "Réponse" });
                 });

        await service.SendPromptAsync(
            new PostPrompt { Prompt = "Question", PromptType = "Completion", ConversationId = conversationId },
            "utilisateur@erabliere.ca",
            CancellationToken.None);

        Assert.NotNull(envoyes);
        var envoye = Assert.Single(envoyes);
        Assert.Equal("Question", envoye.Content[0].Text);
    }

    [Fact]
    public async Task SendPromptAsync_TypeChat_EnveloppeLErreurDuFournisseur()
    {
        var (service, _, aiService) = CreateService();
        aiService.CompleteChatAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatCompletionOptions>(), Arg.Any<CancellationToken>())
                 .Returns<Task<AIResponse?>>(_ => throw new ClientResultException("Le filtre de contenu a rejeté la demande."));

        var exception = await Assert.ThrowsAsync<AIChatCompletionException>(() => service.SendPromptAsync(
            new PostPrompt { Prompt = "Question", PromptType = "Chat" },
            "utilisateur@erabliere.ca",
            CancellationToken.None));

        Assert.Equal("Le filtre de contenu a rejeté la demande.", exception.ClientResult.Message);
    }

    [Fact]
    public async Task SendPromptAsync_TypeCompletion_NEnveloppePasLErreurDuFournisseur()
    {
        var (service, _, aiService) = CreateService();
        aiService.CompleteChatAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatCompletionOptions>(), Arg.Any<CancellationToken>())
                 .Returns<Task<AIResponse?>>(_ => throw new ClientResultException("Le filtre de contenu a rejeté la demande."));

        await Assert.ThrowsAsync<ClientResultException>(() => service.SendPromptAsync(
            new PostPrompt { Prompt = "Question", PromptType = "Completion" },
            "utilisateur@erabliere.ca",
            CancellationToken.None));
    }

    [Fact]
    public async Task SendPromptAsync_ReponseImage_PersisteLUriDeLImage()
    {
        var (service, _, aiService) = CreateService();
        aiService.CompleteChatAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatCompletionOptions>(), Arg.Any<CancellationToken>())
                 .Returns(new AIResponse
                 {
                     Text = "Voici l'image",
                     Kind = ChatMessageContentPartKind.Image.ToString(),
                     ImageUri = new Uri("https://exemple.ca/image.png")
                 });

        var response = await service.SendPromptAsync(
            new PostPrompt { Prompt = "Dessine une érablière", PromptType = "Chat" },
            "utilisateur@erabliere.ca",
            CancellationToken.None);

        Assert.Equal("https://exemple.ca/image.png", response.Response?.ImageUri);
    }

    [Fact]
    public async Task SendPromptAsync_PieceJointeTexte_EstAjouteeAuMessage()
    {
        var (service, _, aiService) = CreateService();

        List<ChatMessage>? envoyes = null;
        aiService.CompleteChatAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatCompletionOptions>(), Arg.Any<CancellationToken>())
                 .Returns(callInfo =>
                 {
                     envoyes = [.. callInfo.Arg<IEnumerable<ChatMessage>>()];
                     return Task.FromResult<AIResponse?>(new AIResponse { Text = "Réponse" });
                 });

        await service.SendPromptAsync(
            new PostPrompt
            {
                Prompt = "Résume ce texte",
                PromptType = "Chat",
                Attachments =
                [
                    new PromptAttachment
                    {
                        FileName = "note.txt",
                        ContentType = "text/plain",
                        TextContent = "Le contenu de la note"
                    }
                ]
            },
            "utilisateur@erabliere.ca",
            CancellationToken.None);

        Assert.NotNull(envoyes);
        var dernier = envoyes[^1];
        Assert.Equal(2, dernier.Content.Count);
        Assert.Equal("Résume ce texte", dernier.Content[0].Text);
        Assert.Equal("Le contenu de la note", dernier.Content[1].Text);
    }

    [Fact]
    public async Task SendPromptAsync_PieceJointeNonSupportee_Leve()
    {
        var (service, _, _) = CreateService();

        await Assert.ThrowsAsync<NotImplementedException>(() => service.SendPromptAsync(
            new PostPrompt
            {
                Prompt = "Résume ce fichier",
                PromptType = "Chat",
                Attachments =
                [
                    new PromptAttachment { FileName = "rapport.pdf", ContentType = "application/pdf" }
                ]
            },
            "utilisateur@erabliere.ca",
            CancellationToken.None));
    }

    [Fact]
    public async Task SendPromptAsync_MetAJourLaDateDuDernierMessage()
    {
        var (service, context, aiService) = CreateService();
        var conversationId = Guid.NewGuid();
        var dateInitiale = DateTimeOffset.Now.AddDays(-5);
        context.Conversations.Add(new Conversation
        {
            Id = conversationId,
            UserId = "utilisateur@erabliere.ca",
            LastMessageDate = dateInitiale
        });
        await context.SaveChangesAsync();

        aiService.CompleteChatAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatCompletionOptions>(), Arg.Any<CancellationToken>())
                 .Returns(new AIResponse { Text = "Réponse" });

        await service.SendPromptAsync(
            new PostPrompt { Prompt = "Question", PromptType = "Chat", ConversationId = conversationId },
            "utilisateur@erabliere.ca",
            CancellationToken.None);

        Assert.True(context.Conversations.Single().LastMessageDate > dateInitiale);
    }

    [Fact]
    public async Task SendPromptAsync_UtiliseLaTemperatureConfiguree()
    {
        var (service, _, aiService) = CreateService();

        ChatCompletionOptions? options = null;
        aiService.CompleteChatAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatCompletionOptions>(), Arg.Any<CancellationToken>())
                 .Returns(callInfo =>
                 {
                     options = callInfo.Arg<ChatCompletionOptions>();
                     return Task.FromResult<AIResponse?>(new AIResponse { Text = "Réponse" });
                 });

        await service.SendPromptAsync(
            new PostPrompt { Prompt = "Question", PromptType = "Chat" },
            "utilisateur@erabliere.ca",
            CancellationToken.None);

        Assert.NotNull(options);
        Assert.Equal(1f, options.Temperature);
        Assert.Equal(0, options.FrequencyPenalty);
        Assert.Equal(0, options.PresencePenalty);
        Assert.False(string.IsNullOrWhiteSpace(options.EndUserId));
    }

    [Fact]
    public async Task SendPromptAsync_SansUtilisateur_NEnvoiePasDEndUserId()
    {
        var (service, _, aiService) = CreateService();

        ChatCompletionOptions? options = null;
        aiService.CompleteChatAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatCompletionOptions>(), Arg.Any<CancellationToken>())
                 .Returns(callInfo =>
                 {
                     options = callInfo.Arg<ChatCompletionOptions>();
                     return Task.FromResult<AIResponse?>(new AIResponse { Text = "Réponse" });
                 });

        await service.SendPromptAsync(
            new PostPrompt { Prompt = "Question", PromptType = "Chat" },
            userId: null,
            CancellationToken.None);

        Assert.NotNull(options);
        Assert.Null(options.EndUserId);
    }

    [Fact]
    public async Task SendPromptAsync_AvecOutils_ExecuteLAppelPuisRepond()
    {
        var harness = CreateHarness();
        EnableTools(harness);

        var seenOptions = ReplyWith(harness,
            ToolCallResponse("call_1"),
            new AIResponse { Text = "Vous avez 3 capteurs." });

        harness.Toolset.InvokeAsync(Arg.Any<AIToolCall>(), Arg.Any<CancellationToken>())
               .Returns(ToolSuccess("call_1"));

        var prompt = new PostPrompt { Prompt = "Combien de capteurs?", PromptType = "Chat" };

        var response = await harness.Service.SendPromptAsync(prompt, "utilisateur@erabliere.ca", CancellationToken.None);

        Assert.Equal("Vous avez 3 capteurs.", response.Response?.Content);
        Assert.Equal(2, seenOptions.Count);
        await harness.Toolset.Received(1).InvokeAsync(Arg.Any<AIToolCall>(), Arg.Any<CancellationToken>());

        // Le tour qui suit l'outil doit encore déclarer les outils : le modèle peut
        // vouloir en appeler un deuxième après avoir lu le premier résultat.
        Assert.Single(seenOptions[1].Tools);
    }

    [Fact]
    public async Task SendPromptAsync_AvecOutils_PersisteLAppelEtLeResultat()
    {
        var harness = CreateHarness();
        EnableTools(harness);

        ReplyWith(harness, ToolCallResponse("call_1"), new AIResponse { Text = "Vous avez 3 capteurs." });

        harness.Toolset.InvokeAsync(Arg.Any<AIToolCall>(), Arg.Any<CancellationToken>())
               .Returns(ToolSuccess("call_1"));

        await harness.Service.SendPromptAsync(
            new PostPrompt { Prompt = "Combien de capteurs?", PromptType = "Chat" },
            "utilisateur@erabliere.ca",
            CancellationToken.None);

        var messages = harness.Context.Messages.OrderBy(m => m.CreatedAt).ToList();

        Assert.Equal(4, messages.Count);
        Assert.Equal(TypesMessage.Texte, messages[0].MessageType);
        Assert.True(messages[0].IsUser);

        Assert.Equal(TypesMessage.AppelOutil, messages[1].MessageType);
        Assert.Equal("list_capteurs", messages[1].ToolName);
        Assert.Equal("call_1", messages[1].ToolCallId);

        Assert.Equal(TypesMessage.ResultatOutil, messages[2].MessageType);
        Assert.Contains("3 capteurs", messages[2].Content);

        Assert.Equal(TypesMessage.Texte, messages[3].MessageType);
        Assert.True(messages[3].UsedLiveData);
    }

    [Fact]
    public async Task SendPromptAsync_AvecOutils_RenvoieLeResultatAuModele()
    {
        var harness = CreateHarness();
        EnableTools(harness);

        var envoyesParAppel = new List<List<ChatMessage>>();

        var call = 0;
        harness.AiService.CompleteChatAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatCompletionOptions>(), Arg.Any<CancellationToken>())
               .Returns(callInfo =>
               {
                   envoyesParAppel.Add([.. callInfo.Arg<IEnumerable<ChatMessage>>()]);

                   return Task.FromResult<AIResponse?>(call++ == 0
                       ? ToolCallResponse("call_1")
                       : new AIResponse { Text = "Vous avez 3 capteurs." });
               });

        harness.Toolset.InvokeAsync(Arg.Any<AIToolCall>(), Arg.Any<CancellationToken>())
               .Returns(ToolSuccess("call_1"));

        await harness.Service.SendPromptAsync(
            new PostPrompt { Prompt = "Combien de capteurs?", PromptType = "Chat" },
            "utilisateur@erabliere.ca",
            CancellationToken.None);

        var secondAppel = envoyesParAppel[1];

        // L'appel d'outil du modèle, puis son résultat : sans ce couple, l'api de
        // complétion refuse la conversation.
        Assert.IsType<AssistantChatMessage>(secondAppel[^2]);
        Assert.Equal("call_1", Assert.Single(((AssistantChatMessage)secondAppel[^2]).ToolCalls).Id);

        var toolMessage = Assert.IsType<ToolChatMessage>(secondAppel[^1]);
        Assert.Equal("call_1", toolMessage.ToolCallId);
        Assert.Contains("3 capteurs", toolMessage.Content[0].Text);
    }

    [Fact]
    public async Task SendPromptAsync_ModeleQuiNArretePas_SArreteAuNombreDeToursMaximal()
    {
        var harness = CreateHarness(options => options.MaxRounds = 2);
        EnableTools(harness);

        // Le modèle redemande un outil à chaque tour, indéfiniment.
        var seenOptions = ReplyWith(harness, ToolCallResponse("call_1"));

        harness.Toolset.InvokeAsync(Arg.Any<AIToolCall>(), Arg.Any<CancellationToken>())
               .Returns(ToolSuccess("call_1"));

        var response = await harness.Service.SendPromptAsync(
            new PostPrompt { Prompt = "Combien de capteurs?", PromptType = "Chat" },
            "utilisateur@erabliere.ca",
            CancellationToken.None);

        await harness.Toolset.Received(2).InvokeAsync(Arg.Any<AIToolCall>(), Arg.Any<CancellationToken>());

        // Deux tours avec outils, puis un dernier tour sans outil : c'est ce qui
        // garantit une réponse plutôt qu'une erreur quand la limite est atteinte.
        Assert.Equal(3, seenOptions.Count);
        Assert.Single(seenOptions[0].Tools);
        Assert.Single(seenOptions[1].Tools);
        Assert.Empty(seenOptions[2].Tools);
        Assert.NotNull(response.Response);
    }

    [Fact]
    public async Task SendPromptAsync_BudgetDeJetonsDepasse_ArreteDAppelerLesOutils()
    {
        var harness = CreateHarness(options =>
        {
            options.MaxRounds = 5;
            options.TokenBudget = 10;
        });
        EnableTools(harness);

        var seenOptions = ReplyWith(harness, ToolCallResponse("call_1"));

        // Un seul résultat coûte déjà plus que le budget.
        harness.Toolset.InvokeAsync(Arg.Any<AIToolCall>(), Arg.Any<CancellationToken>())
               .Returns(ToolSuccess("call_1"));

        await harness.Service.SendPromptAsync(
            new PostPrompt { Prompt = "Combien de capteurs?", PromptType = "Chat" },
            "utilisateur@erabliere.ca",
            CancellationToken.None);

        await harness.Toolset.Received(1).InvokeAsync(Arg.Any<AIToolCall>(), Arg.Any<CancellationToken>());
        Assert.Equal(2, seenOptions.Count);
        Assert.Empty(seenOptions[1].Tools);
    }

    [Fact]
    public async Task SendPromptAsync_OutilEnErreur_RepondQuandMemeSansMarquerLesDonneesReelles()
    {
        var harness = CreateHarness();
        EnableTools(harness);

        ReplyWith(harness,
            ToolCallResponse("call_1"),
            new AIResponse { Text = "Je n'ai pas pu lire vos capteurs." });

        harness.Toolset.InvokeAsync(Arg.Any<AIToolCall>(), Arg.Any<CancellationToken>())
               .Returns(new ErabliereAiToolResult(
                   "call_1", "list_capteurs", "{}", """{"error":"No maple grove found."}""", IsError: true, EstimatedTokens: 8));

        var response = await harness.Service.SendPromptAsync(
            new PostPrompt { Prompt = "Combien de capteurs?", PromptType = "Chat" },
            "utilisateur@erabliere.ca",
            CancellationToken.None);

        Assert.Equal("Je n'ai pas pu lire vos capteurs.", response.Response?.Content);

        // La pastille « données réelles » ne doit décorer que ce qui a vraiment été lu.
        Assert.False(response.Response?.UsedLiveData);

        // L'échec est tout de même conservé : c'est la trace de ce qui a été tenté.
        Assert.Contains(harness.Context.Messages, m => m.MessageType == TypesMessage.ResultatOutil);
    }

    [Fact]
    public async Task SendPromptAsync_ForfaitSansAcces_NOffreAucunOutil()
    {
        var harness = CreateHarness();
        harness.AiService.SupportsToolCalling.Returns(true);
        harness.Toolset.GetChatTools().Returns([FakeTool]);

        harness.Capabilities.GetCapabilitiesAsync(Arg.Any<CancellationToken>())
               .Returns(new ErabliereAiCapabilities(false, "gratuit", true, ["base"], null));

        var seenOptions = ReplyWith(harness, new AIResponse { Text = "Réponse" });

        await harness.Service.SendPromptAsync(
            new PostPrompt { Prompt = "Combien de capteurs?", PromptType = "Chat" },
            "utilisateur@erabliere.ca",
            CancellationToken.None);

        Assert.Empty(Assert.Single(seenOptions).Tools);
        await harness.Toolset.DidNotReceive().InvokeAsync(Arg.Any<AIToolCall>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendPromptAsync_FournisseurSansAppelDOutil_NeConsultePasMemeLeForfait()
    {
        // Chemin Gemini : la conversation doit fonctionner exactement comme avant.
        var harness = CreateHarness();
        harness.AiService.SupportsToolCalling.Returns(false);

        var seenOptions = ReplyWith(harness, new AIResponse { Text = "Réponse" });

        await harness.Service.SendPromptAsync(
            new PostPrompt { Prompt = "Combien de capteurs?", PromptType = "Chat" },
            "utilisateur@erabliere.ca",
            CancellationToken.None);

        Assert.Empty(Assert.Single(seenOptions).Tools);
        await harness.Capabilities.DidNotReceive().GetCapabilitiesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendPromptAsync_HistoriqueAvecMessagesDOutil_NeLesRejouePas()
    {
        var harness = CreateHarness();
        var conversationId = Guid.NewGuid();

        harness.Context.Conversations.Add(new Conversation { Id = conversationId, UserId = "utilisateur@erabliere.ca" });
        harness.Context.Messages.Add(new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Content = "Combien de capteurs?",
            IsUser = true,
            MessageType = TypesMessage.Texte,
            CreatedAt = DateTimeOffset.Now.AddMinutes(-3)
        });
        harness.Context.Messages.Add(new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Content = "{}",
            IsUser = false,
            MessageType = TypesMessage.AppelOutil,
            ToolName = "list_capteurs",
            ToolCallId = "call_1",
            CreatedAt = DateTimeOffset.Now.AddMinutes(-2)
        });
        harness.Context.Messages.Add(new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Content = """{"summary":"3 capteurs"}""",
            IsUser = false,
            MessageType = TypesMessage.ResultatOutil,
            ToolName = "list_capteurs",
            ToolCallId = "call_1",
            CreatedAt = DateTimeOffset.Now.AddMinutes(-1)
        });
        harness.Context.Messages.Add(new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Content = "Vous avez 3 capteurs.",
            IsUser = false,
            MessageType = TypesMessage.Texte,
            UsedLiveData = true,
            CreatedAt = DateTimeOffset.Now
        });
        await harness.Context.SaveChangesAsync();

        List<ChatMessage>? envoyes = null;
        harness.AiService.CompleteChatAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatCompletionOptions>(), Arg.Any<CancellationToken>())
               .Returns(callInfo =>
               {
                   envoyes = [.. callInfo.Arg<IEnumerable<ChatMessage>>()];
                   return Task.FromResult<AIResponse?>(new AIResponse { Text = "Réponse" });
               });

        await harness.Service.SendPromptAsync(
            new PostPrompt { Prompt = "Et des alertes?", PromptType = "Chat", ConversationId = conversationId },
            "utilisateur@erabliere.ca",
            CancellationToken.None);

        Assert.NotNull(envoyes);

        // Phrase système, question, réponse, nouvelle question : les deux messages
        // d'outil sont conservés en base pour l'interface, jamais renvoyés au modèle.
        Assert.Equal(4, envoyes.Count);
        Assert.DoesNotContain(envoyes, m => m is ToolChatMessage);
    }

    [Fact]
    public async Task SendPromptAsync_AvecOutils_PublieLActiviteEtLaTermine()
    {
        var harness = CreateHarness();
        EnableTools(harness);

        var tracker = Substitute.For<IToolActivityTracker>();

        var service = new ConversationAIService(
            harness.Context,
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["LLMDefaultTemperature"] = "1" }).Build(),
            harness.AiService,
            new SystemPromptBuilder(),
            harness.Toolset,
            harness.Capabilities,
            tracker,
            Options.Create(harness.Options),
            NullLogger<ConversationAIService>.Instance);

        ReplyWith(harness, ToolCallResponse("call_1"), new AIResponse { Text = "Réponse" });
        harness.Toolset.InvokeAsync(Arg.Any<AIToolCall>(), Arg.Any<CancellationToken>()).Returns(ToolSuccess("call_1"));

        var activityId = Guid.NewGuid();

        await service.SendPromptAsync(
            new PostPrompt { Prompt = "Combien de capteurs?", PromptType = "Chat", ActivityId = activityId },
            "utilisateur@erabliere.ca",
            CancellationToken.None);

        tracker.Received().Publish(activityId, Arg.Is<ToolActivityStep>(s => s.ToolName == "list_capteurs"));
        tracker.Received(1).Complete(activityId);
    }

    [Fact]
    public async Task SendPromptAsync_ErreurDuFournisseur_TermineQuandMemeLActivite()
    {
        var harness = CreateHarness();
        var tracker = Substitute.For<IToolActivityTracker>();

        var service = new ConversationAIService(
            harness.Context,
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["LLMDefaultTemperature"] = "1" }).Build(),
            harness.AiService,
            new SystemPromptBuilder(),
            harness.Toolset,
            harness.Capabilities,
            tracker,
            Options.Create(harness.Options),
            NullLogger<ConversationAIService>.Instance);

        harness.AiService.CompleteChatAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatCompletionOptions>(), Arg.Any<CancellationToken>())
               .Returns<Task<AIResponse?>>(_ => throw new ClientResultException("Refusé."));

        var activityId = Guid.NewGuid();

        await Assert.ThrowsAsync<AIChatCompletionException>(() => service.SendPromptAsync(
            new PostPrompt { Prompt = "Question", PromptType = "Chat", ActivityId = activityId },
            "utilisateur@erabliere.ca",
            CancellationToken.None));

        // Sinon le client interrogerait le statut jusqu'à l'expiration du cache.
        tracker.Received(1).Complete(activityId);
    }

    private static (IConversationAIService service, ErabliereDbContext context, IAIService aiService) CreateService()
    {
        var harness = CreateHarness();

        return (harness.Service, harness.Context, harness.AiService);
    }

    /// <summary>
    /// Everything a <see cref="ConversationAIService" /> needs, with the tools off by
    /// default: a substituted provider reports no tool calling support, which is what
    /// keeps the pre tool behaviour under test.
    /// </summary>
    private sealed record Harness(
        IConversationAIService Service,
        ErabliereDbContext Context,
        IAIService AiService,
        IErabliereAiToolset Toolset,
        IErabliereAiCapabilityService Capabilities,
        ErabliereAiToolOptions Options);

    private static Harness CreateHarness(Action<ErabliereAiToolOptions>? configureTools = null)
    {
        var dbOptions = new DbContextOptionsBuilder()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new ErabliereDbContext(dbOptions);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Valeur entière volontairement : GetRequiredValue utilise la culture courante.
                ["LLMDefaultTemperature"] = "1"
            })
            .Build();

        var aiService = Substitute.For<IAIService>();
        var toolset = Substitute.For<IErabliereAiToolset>();
        var capabilities = Substitute.For<IErabliereAiCapabilityService>();

        var toolOptions = new ErabliereAiToolOptions();
        configureTools?.Invoke(toolOptions);

        var service = new ConversationAIService(
            context,
            configuration,
            aiService,
            new SystemPromptBuilder(),
            toolset,
            capabilities,
            Substitute.For<IToolActivityTracker>(),
            Options.Create(toolOptions),
            NullLogger<ConversationAIService>.Instance);

        return new Harness(service, context, aiService, toolset, capabilities, toolOptions);
    }

    /// <summary>
    /// Turns the tools on: a provider that supports them, a plan that grants them and
    /// one declared tool.
    /// </summary>
    private static void EnableTools(Harness harness)
    {
        harness.AiService.SupportsToolCalling.Returns(true);

        harness.Capabilities.GetCapabilitiesAsync(Arg.Any<CancellationToken>())
               .Returns(new ErabliereAiCapabilities(true, "base", true, ["base"], null));

        harness.Toolset.GetChatTools().Returns([FakeTool]);
    }

    private static readonly ChatTool FakeTool = ChatTool.CreateFunctionTool(
        "list_capteurs",
        "Liste les capteurs.",
        BinaryData.FromString("""{"type":"object","properties":{}}"""));

    private static AIResponse ToolCallResponse(string toolCallId, string toolName = "list_capteurs", string arguments = "{}")
    {
        return new AIResponse
        {
            FinishReason = "ToolCalls",
            ToolCalls = [new AIToolCall(toolCallId, toolName, arguments)]
        };
    }

    private static ErabliereAiToolResult ToolSuccess(string toolCallId, string resultJson = """{"summary":"3 capteurs","data":[],"truncated":false}""")
    {
        return new ErabliereAiToolResult(toolCallId, "list_capteurs", "{}", resultJson, IsError: false, EstimatedTokens: 20);
    }

    /// <summary>
    /// Replays the given answers, one per call, repeating the last one forever after.
    /// </summary>
    private static List<ChatCompletionOptions> ReplyWith(Harness harness, params AIResponse[] answers)
    {
        var seenOptions = new List<ChatCompletionOptions>();
        var call = 0;

        harness.AiService.CompleteChatAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatCompletionOptions>(), Arg.Any<CancellationToken>())
               .Returns(callInfo =>
               {
                   seenOptions.Add(callInfo.Arg<ChatCompletionOptions>());

                   var answer = answers[Math.Min(call, answers.Length - 1)];
                   call++;

                   return Task.FromResult<AIResponse?>(answer);
               });

        return seenOptions;
    }
}
