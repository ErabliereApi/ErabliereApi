using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Donnees.Action.Post;
using ErabliereApi.Services.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

    private static (IConversationAIService service, ErabliereDbContext context, IAIService aiService) CreateService()
    {
        var options = new DbContextOptionsBuilder()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new ErabliereDbContext(options);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Valeur entière volontairement : GetRequiredValue utilise la culture courante.
                ["LLMDefaultTemperature"] = "1"
            })
            .Build();

        var aiService = Substitute.For<IAIService>();

        var service = new ConversationAIService(context, configuration, aiService, new SystemPromptBuilder());

        return (service, context, aiService);
    }
}
