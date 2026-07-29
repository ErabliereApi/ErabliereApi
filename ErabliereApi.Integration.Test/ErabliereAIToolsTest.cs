using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Donnees.Action.Get;
using ErabliereApi.Donnees.Action.Post;
using ErabliereApi.Donnees.Contantes;
using ErabliereApi.Integration.Test.ApplicationFactory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace ErabliereApi.Integration.Test;

/// <summary>
/// ErabliereAI répondant avec les données réelles de l'utilisateur, du prompt
/// jusqu'à la lecture en base et retour.
/// </summary>
/// <remarks>
/// L'assistant ne détient aucune identité : ses outils rappellent l'API en
/// transportant les identifiants de la requête en cours. Ce fichier vérifie les
/// deux faces de cette affirmation — l'utilisateur atteint ses propres données, et
/// il n'atteint pas celles d'un autre — en laissant les outils traverser
/// l'authentification et les filtres de propriété réels.
/// </remarks>
public class ErabliereAIToolsTest : IClassFixture<ErabliereAIApplicationFactory<Startup>>
{
    private readonly ErabliereAIApplicationFactory<Startup> _factory;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ErabliereAIToolsTest(ErabliereAIApplicationFactory<Startup> factory)
    {
        _factory = factory;
        _factory.AI.Reset();
    }

    [Fact]
    public async Task UnPrompt_ExecuteLOutilPuisRepondAvecLesDonneesLues()
    {
        var (_, cle, erabliere) = await CreerUtilisateurAvecErabliereAsync();

        _factory.AI
            .EnqueueToolCall("list_erablieres", "{}")
            .EnqueueAnswer($"Vous avez une érablière : {erabliere}.");

        var reponse = await EnvoyerPromptAsync(cle, "Quelles sont mes érablières?");

        Assert.Equal($"Vous avez une érablière : {erabliere}.", reponse.Response?.Content);

        // Le résultat de l'outil est bien passé par l'API : le nom vient de la base,
        // pas du scénario.
        Assert.Contains(_factory.AI.ToolResultsSentToTheModel(), result => result.Contains(erabliere));

        var messages = await LireMessagesAsync(reponse.Conversation!.Id!.Value);

        Assert.Collection(messages,
            m => Assert.Equal(TypesMessage.Texte, m.MessageType),
            m => Assert.Equal(TypesMessage.AppelOutil, m.MessageType),
            m => Assert.Equal(TypesMessage.ResultatOutil, m.MessageType),
            m => Assert.Equal(TypesMessage.Texte, m.MessageType));

        Assert.True(messages[^1].UsedLiveData);
        Assert.Equal("list_erablieres", messages[1].ToolName);
    }

    [Fact]
    public async Task UnUtilisateur_NAtteintPasLErabliereDUnAutreParLaConversation()
    {
        var (_, cleDeA, erabliereDeA) = await CreerUtilisateurAvecErabliereAsync();
        var (_, _, erabliereDeB) = await CreerUtilisateurAvecErabliereAsync();

        var idErabliereDeB = await TrouverIdErabliereAsync(erabliereDeB);

        // Le modèle demande explicitement l'érablière du voisin : c'est le scénario
        // qu'une injection de prompt produirait.
        _factory.AI
            .EnqueueToolCall("get_erabliere", $$"""{"erabliereId":"{{idErabliereDeB}}"}""")
            .EnqueueAnswer("Je n'ai pas pu lire cette érablière.");

        var reponse = await EnvoyerPromptAsync(cleDeA, $"Montre-moi l'érablière {idErabliereDeB}.");

        var resultat = Assert.Single(_factory.AI.ToolResultsSentToTheModel().ToList());

        // Rien de l'érablière de B ne doit avoir traversé, ni son nom ni un résumé.
        Assert.DoesNotContain(erabliereDeB, resultat);

        // Et le refus doit venir de la propriété : le message d'un outil abandonné ou
        // d'une panne ferait passer ce test sans rien prouver.
        Assert.Contains("No maple grove found", resultat);

        var messages = await LireMessagesAsync(reponse.Conversation!.Id!.Value);

        Assert.DoesNotContain(messages, m => m.Content != null && m.Content.Contains(erabliereDeB));
        Assert.False(messages[^1].UsedLiveData);

        // Contre épreuve, dans les mêmes conditions : le même outil, appelé par le
        // même utilisateur sur sa propre érablière, répond. L'assertion ci-dessus
        // porte donc bien sur la propriété et non sur des outils inertes.
        var idErabliereDeA = await TrouverIdErabliereAsync(erabliereDeA);

        _factory.AI.Reset();
        _factory.AI
            .EnqueueToolCall("get_erabliere", $$"""{"erabliereId":"{{idErabliereDeA}}"}""")
            .EnqueueAnswer("Voici votre érablière.");

        await EnvoyerPromptAsync(cleDeA, "Montre-moi mon érablière.");

        Assert.Contains(erabliereDeA, Assert.Single(_factory.AI.ToolResultsSentToTheModel().ToList()));
    }

    [Fact]
    public async Task UnUtilisateur_NeListeQueSesPropresErablieres()
    {
        var (_, cleDeA, erabliereDeA) = await CreerUtilisateurAvecErabliereAsync();
        var (_, _, erabliereDeB) = await CreerUtilisateurAvecErabliereAsync();

        _factory.AI
            .EnqueueToolCall("list_erablieres", "{}")
            .EnqueueAnswer("Voici vos érablières.");

        await EnvoyerPromptAsync(cleDeA, "Quelles sont mes érablières?");

        var resultat = Assert.Single(_factory.AI.ToolResultsSentToTheModel().ToList());

        Assert.Contains(erabliereDeA, resultat);
        Assert.DoesNotContain(erabliereDeB, resultat);
    }

    [Fact]
    public async Task UnOutilDEcriture_NEstJamaisDeclareAuModele()
    {
        var (_, cle, _) = await CreerUtilisateurAvecErabliereAsync();

        _factory.AI.EnqueueAnswer("Bonjour.");

        await EnvoyerPromptAsync(cle, "Bonjour");

        var outils = _factory.AI.Options[0].Tools.Select(t => t.FunctionName).ToList();

        Assert.NotEmpty(outils);
        Assert.DoesNotContain(outils, nom => nom.StartsWith("post_") || nom.StartsWith("delete_") || nom.StartsWith("put_"));
        Assert.DoesNotContain("get_my_plan", outils);
    }

    [Fact]
    public async Task LesCapacites_AnnoncentQueLesOutilsSontOuverts()
    {
        var (_, cle, _) = await CreerUtilisateurAvecErabliereAsync();

        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/ErabliereAI/Capabilities");
        request.Headers.Add("X-ErabliereApi-ApiKey", cle);

        using var response = await client.SendAsync(request);

        var contenu = await response.Content.ReadAsStringAsync();

        // Une réponse en HTML signifie que la route n'existe pas et que le repli SPA
        // a répondu : c'est ce qui arrive quand ErabliereAIController est filtré.
        Assert.True(response.StatusCode == HttpStatusCode.OK && !contenu.StartsWith('<'),
            $"GET /ErabliereAI/Capabilities a répondu {response.StatusCode} : {Tronquer(contenu)}");

        var capacites = JsonSerializer.Deserialize<GetErabliereAICapabilities>(contenu, JsonOptions);

        Assert.NotNull(capacites);

        // La porte par forfait est fermée dans cette fabrique : tout le monde entre.
        Assert.True(capacites.ToolsEnabled);
        Assert.False(capacites.PlanGateEnabled);
    }

    /// <summary>
    /// Crée un utilisateur, sa clé d'api et une érablière privée lui appartenant.
    /// L'érablière est créée par l'API elle-même, donc la propriété est établie
    /// exactement comme en production.
    /// </summary>
    private async Task<(Donnees.Customer customer, string cle, string nomErabliere)> CreerUtilisateurAvecErabliereAsync()
    {
        var (customer, cle) = await _factory.CreateValidApiKeyAsync();

        var nom = $"Sucrerie-{Guid.NewGuid()}";

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-ErabliereApi-ApiKey", cle);

        using var response = await client.PostAsJsonAsync("/Erablieres", new PostErabliere
        {
            Nom = nom,
            AfficherSectionBaril = true,
            AfficherSectionDompeux = true,
            AfficherTrioDonnees = true,
            IndiceOrdre = 0,
            IpRules = "-",
            // Privée : une érablière publique serait lisible par tous, et le test
            // d'isolation ne prouverait rien.
            IsPublic = false,
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return (customer, cle, nom);
    }

    private async Task<Guid> TrouverIdErabliereAsync(string nom)
    {
        using var scope = _factory.Services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ErabliereDbContext>();

        var erabliere = await context.Erabliere.AsNoTracking().FirstAsync(e => e.Nom == nom);

        return erabliere.Id!.Value;
    }

    private async Task<PostPromptResponse> EnvoyerPromptAsync(string cle, string question)
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-ErabliereApi-ApiKey", cle);

        using var response = await client.PostAsJsonAsync("/ErabliereAI/Prompt", new PostPrompt
        {
            Prompt = question,
            PromptType = "Chat"
        });

        var contenu = await response.Content.ReadAsStringAsync();

        // Le corps est dans le message : « Actual: InternalServerError » tout seul
        // n'apprend rien, et ces tests échouent surtout pour des raisons d'hôte.
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"POST /ErabliereAI/Prompt a répondu {response.StatusCode} : {Tronquer(contenu)}");

        var body = JsonSerializer.Deserialize<PostPromptResponse>(contenu, JsonOptions);

        Assert.NotNull(body);
        Assert.NotNull(body.Conversation);

        return body;
    }

    private static string Tronquer(string contenu)
    {
        return contenu.Length <= 800 ? contenu : contenu[..800] + "…";
    }

    private async Task<List<Message>> LireMessagesAsync(Guid conversationId)
    {
        using var scope = _factory.Services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ErabliereDbContext>();

        return await context.Messages.AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }
}
