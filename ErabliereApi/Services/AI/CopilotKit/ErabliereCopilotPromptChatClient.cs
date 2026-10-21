using AGUI.Server;
using ErabliereApi.Services.AI.Tools;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace ErabliereApi.Services.AI.CopilotKit;

/// <summary>
/// Gives every completion of the CopilotKit agent the system prompt of the
/// ErabliereAI chat, and only the tools the caller's plan opens.
/// </summary>
/// <remarks>
/// Sits under the function invocation layer, so it runs on every round of a tool
/// loop. What it reads per request — the plan and the sensors of the maple grove —
/// is resolved once and kept on the <see cref="HttpContext" />.
/// <para>
/// The maple grove comes from the AG-UI context the browser sends, entry
/// <see cref="ErabliereContextDescription" />. Like <c>PostPrompt.ErabliereId</c>, it
/// is never an authorization input: it is written into the prompt so the model stops
/// guessing identifiers, and the sensors are read as the caller.
/// </para>
/// </remarks>
public sealed class ErabliereCopilotPromptChatClient : DelegatingChatClient
{
    /// <summary>
    /// Description of the AG-UI context entry carrying the maple grove the user is
    /// looking at, as <c>{"id": "...", "nom": "..."}</c>.
    /// </summary>
    public const string ErabliereContextDescription = "erabliere";

    private static readonly object PreparedKey = new();

    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Constructeur par initialisation
    /// </summary>
    public ErabliereCopilotPromptChatClient(IChatClient innerClient, IHttpContextAccessor httpContextAccessor)
        : base(innerClient)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var (preparedMessages, preparedOptions) = await PrepareAsync(messages, options, cancellationToken);

        var response = await base.GetResponseAsync(preparedMessages, preparedOptions, cancellationToken);

        foreach (var message in response.Messages)
        {
            EnsureCallIds(message.Contents);
        }

        return response;
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var (preparedMessages, preparedOptions) = await PrepareAsync(messages, options, cancellationToken);

        await foreach (var update in base.GetStreamingResponseAsync(preparedMessages, preparedOptions, cancellationToken))
        {
            EnsureCallIds(update.Contents);

            yield return update;
        }
    }

    /// <summary>
    /// Gives an identifier to the tool calls that came without one.
    /// </summary>
    /// <remarks>
    /// Gemini does not identify its function calls, and AG-UI pairs a call with its
    /// result — and CopilotKit renders it — by that identifier: two calls of one round
    /// would otherwise share the empty one.
    /// </remarks>
    private static void EnsureCallIds(IList<AIContent> contents)
    {
        for (var i = 0; i < contents.Count; i++)
        {
            if (contents[i] is FunctionCallContent { CallId: null or "" } call)
            {
                contents[i] = new FunctionCallContent($"call_{Guid.NewGuid():N}", call.Name, call.Arguments)
                {
                    Exception = call.Exception,
                    RawRepresentation = call.RawRepresentation,
                    AdditionalProperties = call.AdditionalProperties
                };
            }
        }
    }

    private async Task<(List<ChatMessage> Messages, ChatOptions? Options)> PrepareAsync(IEnumerable<ChatMessage> messages, ChatOptions? options, CancellationToken token)
    {
        var httpContext = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("The CopilotKit agent only runs within the http request of the AG-UI endpoint.");

        var prepared = await GetOrPrepareAsync(httpContext, options, token);

        // The system prompt is the server's: whatever system message the browser sent
        // is dropped rather than trusted.
        var preparedMessages = new List<ChatMessage>
        {
            new(ChatRole.System, prepared.SystemPrompt)
        };
        preparedMessages.AddRange(messages.Where(m => m.Role != ChatRole.System));

        if (options is null)
        {
            return (preparedMessages, options);
        }

        var preparedOptions = options.Clone();

        if (!prepared.ToolsEnabled && preparedOptions.Tools is { Count: > 0 } tools)
        {
            // Only the tools of the catalog are withdrawn: the tools the browser
            // declared run in the browser, with nothing to read on the server.
            var catalog = httpContext.RequestServices.GetRequiredService<ErabliereAiToolCatalog>();

            preparedOptions.Tools = [.. tools.Where(t => !catalog.ToolNames.Contains(t.Name))];
        }

        if (prepared.ToolsEnabled)
        {
            preparedOptions.Temperature ??= prepared.Temperature;
        }

        return (preparedMessages, preparedOptions);
    }

    private static async Task<PreparedRequest> GetOrPrepareAsync(HttpContext httpContext, ChatOptions? options, CancellationToken token)
    {
        if (httpContext.Items.TryGetValue(PreparedKey, out var cached) && cached is PreparedRequest preparedRequest)
        {
            return preparedRequest;
        }

        var services = httpContext.RequestServices;

        var capabilities = await services.GetRequiredService<IErabliereAiCapabilityService>().GetCapabilitiesAsync(token);

        var (erabliereId, erabliereNom) = ReadErabliere(options);

        var capteurs = capabilities.ToolsEnabled && erabliereId is { } id ?
            await services.GetRequiredService<IErabliereAiCapteurCatalog>().ReadAsync(id, token) :
            [];

        var systemPrompt = services.GetRequiredService<ISystemPromptBuilder>().BuildForCompletion(
            null,
            new ErabliereAiPromptContext(capabilities.ToolsEnabled, erabliereId, erabliereNom, capteurs));

        var prepared = new PreparedRequest(
            systemPrompt,
            capabilities.ToolsEnabled,
            services.GetRequiredService<IOptions<ErabliereAiToolOptions>>().Value.Temperature);

        httpContext.Items[PreparedKey] = prepared;

        return prepared;
    }

    /// <summary>
    /// The maple grove of the AG-UI context, or nothing when the entry is absent or
    /// not the expected shape. A malformed entry costs the prompt its maple grove,
    /// never the request.
    /// </summary>
    private static (Guid? Id, string? Nom) ReadErabliere(ChatOptions? options)
    {
        if (options is null || !options.TryGetRunAgentInput(out var input) || input.Context is null)
        {
            return (null, null);
        }

        var entry = input.Context.FirstOrDefault(c => string.Equals(c.Description, ErabliereContextDescription, StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(entry?.Value))
        {
            return (null, null);
        }

        try
        {
            using var json = JsonDocument.Parse(entry.Value);

            var root = json.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return (null, null);
            }

            Guid? id = root.TryGetProperty("id", out var idProperty) && idProperty.ValueKind == JsonValueKind.String && Guid.TryParse(idProperty.GetString(), out var parsed) ?
                parsed :
                null;

            string? nom = root.TryGetProperty("nom", out var nomProperty) && nomProperty.ValueKind == JsonValueKind.String ?
                nomProperty.GetString() :
                null;

            return (id, nom);
        }
        catch (JsonException)
        {
            return (null, null);
        }
    }

    private sealed record PreparedRequest(string SystemPrompt, bool ToolsEnabled, float? Temperature);
}
