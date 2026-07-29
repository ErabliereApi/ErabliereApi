using ErabliereApi.Services.AI;
using OpenAI.Chat;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ErabliereApi.Integration.Test.ApplicationFactory;

/// <summary>
/// Un <see cref="IAIService" /> qui joue une suite de réponses préparées.
///
/// Il permet de décider ce que « le modèle » demande — y compris de demander un
/// outil sur une érablière qui ne lui appartient pas — sans dépendre d'un vrai
/// fournisseur ni de son bon vouloir.
/// </summary>
public class ScriptedAIService : IAIService
{
    private readonly ConcurrentQueue<AIResponse> _responses = new();

    /// <inheritdoc />
    public bool SupportsToolCalling { get; set; } = true;

    /// <summary>
    /// Les conversations envoyées au fournisseur, un élément par appel.
    /// </summary>
    public List<List<ChatMessage>> Calls { get; } = [];

    /// <summary>
    /// Les options de chaque appel, pour vérifier quels outils ont été déclarés.
    /// </summary>
    public List<ChatCompletionOptions> Options { get; } = [];

    /// <summary>
    /// Prépare la prochaine réponse du modèle.
    /// </summary>
    public ScriptedAIService Enqueue(AIResponse response)
    {
        _responses.Enqueue(response);

        return this;
    }

    /// <summary>
    /// Prépare une demande d'appel d'outil.
    /// </summary>
    public ScriptedAIService EnqueueToolCall(string toolName, string argumentsJson, string toolCallId = "call_1")
    {
        return Enqueue(new AIResponse
        {
            FinishReason = "ToolCalls",
            ToolCalls = [new AIToolCall(toolCallId, toolName, argumentsJson)]
        });
    }

    /// <summary>
    /// Prépare une réponse textuelle finale.
    /// </summary>
    public ScriptedAIService EnqueueAnswer(string text)
    {
        return Enqueue(new AIResponse { Text = text });
    }

    /// <summary>
    /// Vide le scénario et l'historique entre deux tests partageant la fabrique.
    /// </summary>
    public void Reset()
    {
        _responses.Clear();
        Calls.Clear();
        Options.Clear();
        SupportsToolCalling = true;
    }

    /// <summary>
    /// Le contenu des messages d'outil renvoyés au modèle, dans l'ordre.
    /// </summary>
    public IEnumerable<string> ToolResultsSentToTheModel()
    {
        return Calls.SelectMany(call => call)
                    .OfType<ToolChatMessage>()
                    .Select(message => message.Content[0].Text);
    }

    /// <inheritdoc />
    public Task<AIResponse?> CompleteChatAsync(IEnumerable<ChatMessage> messages, ChatCompletionOptions chatCompletion, CancellationToken token)
    {
        Calls.Add([.. messages]);
        Options.Add(chatCompletion);

        if (!_responses.TryDequeue(out var response))
        {
            // Le scénario est épuisé : le modèle conclut. Sans cela, une boucle
            // d'outils mal bornée tournerait jusqu'à sa limite en silence.
            response = new AIResponse { Text = "Fin du scénario." };
        }

        return Task.FromResult<AIResponse?>(response);
    }
}
