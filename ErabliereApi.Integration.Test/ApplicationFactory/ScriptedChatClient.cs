using Microsoft.Extensions.AI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ErabliereApi.Integration.Test.ApplicationFactory;

/// <summary>
/// Un <see cref="IChatClient" /> qui joue un scénario plutôt que d'appeler un modèle,
/// le pendant Microsoft.Extensions.AI de <see cref="ScriptedAIService" />.
/// </summary>
public class ScriptedChatClient : IChatClient
{
    private readonly ConcurrentQueue<Func<ChatResponseUpdate>> _script = new();

    /// <summary>
    /// Les messages reçus à chaque appel, dans l'ordre.
    /// </summary>
    public ConcurrentQueue<List<ChatMessage>> Calls { get; } = new();

    /// <summary>
    /// Les options reçues à chaque appel, dans l'ordre.
    /// </summary>
    public ConcurrentQueue<ChatOptions?> Options { get; } = new();

    public ScriptedChatClient EnqueueToolCall(string toolName, string argumentsJson)
    {
        _script.Enqueue(() => new ChatResponseUpdate(ChatRole.Assistant,
        [
            new FunctionCallContent(
                $"call_{Guid.NewGuid():N}",
                toolName,
                JsonSerializer.Deserialize<Dictionary<string, object?>>(argumentsJson))
        ])
        {
            MessageId = $"msg_{Guid.NewGuid():N}"
        });

        return this;
    }

    public ScriptedChatClient EnqueueAnswer(string text)
    {
        _script.Enqueue(() => new ChatResponseUpdate(ChatRole.Assistant, text)
        {
            MessageId = $"msg_{Guid.NewGuid():N}"
        });

        return this;
    }

    public void Reset()
    {
        _script.Clear();
        Calls.Clear();
        Options.Clear();
    }

    /// <summary>
    /// Le contenu des résultats d'outils que le modèle a reçus, tous appels confondus.
    /// </summary>
    public IEnumerable<string> ToolResultsSentToTheModel()
    {
        return Calls.SelectMany(messages => messages)
                    .SelectMany(m => m.Contents.OfType<FunctionResultContent>())
                    .Select(r => r.Result is JsonElement json ? json.GetRawText() : r.Result?.ToString() ?? "")
                    .Distinct();
    }

    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await GetStreamingResponseAsync(messages, options, cancellationToken).ToChatResponseAsync(cancellationToken);
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Calls.Enqueue([.. messages]);
        Options.Enqueue(options);

        await Task.Yield();

        if (!_script.TryDequeue(out var next))
        {
            throw new InvalidOperationException("Le scénario du modèle est épuisé : le test attendait moins d'appels.");
        }

        yield return next();
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return serviceType.IsInstanceOfType(this) ? this : null;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
