using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Messages;
using OpenAI.Chat;
using System.ClientModel;

namespace ErabliereApi.Services.AI;

/// <summary>
/// Service pour interagir avec l'api de messages d'Anthropic (Claude)
/// </summary>
public class AnthropicAIService : IAIService
{
    private readonly IConfiguration _config;

    /// <summary>
    /// Le nombre maximal de jetons de sortie quand <c>AnthropicMaxTokens</c> n'est
    /// pas configuré. L'api d'Anthropic exige ce paramètre, contrairement aux deux
    /// autres fournisseurs, et les jetons de réflexion du modèle comptent dedans :
    /// une valeur plus basse tronquerait la réponse au milieu de sa pensée.
    /// </summary>
    public const int DefaultMaxTokens = 16000;

    /// <summary>
    /// Le modèle utilisé quand <c>AnthropicModel</c> n'est pas configuré.
    /// </summary>
    public const string DefaultModel = "claude-opus-5";

    /// <summary>
    /// Constructeur
    /// </summary>
    /// <param name="config"></param>
    public AnthropicAIService(IConfiguration config)
    {
        _config = config;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Both halves of the loop are implemented by <see cref="AnthropicRequestMapper" />:
    /// the tool declarations and the tool_use blocks of the answer are translated, and
    /// so are the assistant and tool messages carrying the results on the next turn.
    /// Set <c>AnthropicEnableToolCalling</c> to "false" to take the tools away from
    /// this provider without changing the rest of the configuration; the chat then
    /// answers from the model's own knowledge, as it did before tool calling existed.
    /// </remarks>
    public bool SupportsToolCalling =>
        !string.Equals(_config["AnthropicEnableToolCalling"], "false", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    /// <remarks>
    /// The temperature and penalty knobs of <paramref name="chatCompletion" /> are
    /// deliberately not forwarded: Anthropic has no frequency or presence penalty,
    /// and the current Claude models reject a temperature with a 400. The lowered
    /// temperature the tool loop asks for during a tool exchange is therefore a
    /// no-op with this provider. A provider error is rethrown as
    /// <see cref="ClientResultException" />, the type the rest of the pipeline
    /// translates into a 400 for the caller — the same path Azure OpenAI errors
    /// take.
    /// </remarks>
    public async Task<AIResponse?> CompleteChatAsync(IEnumerable<ChatMessage> messages, ChatCompletionOptions chatCompletion, CancellationToken token)
    {
        var client = new AnthropicClient { ApiKey = _config["AnthropicApiKey"] };
        var system = AnthropicRequestMapper.MapSystem(messages);

        try
        {
            var response = await client.Messages.Create(new MessageCreateParams
            {
                Model = _config["AnthropicModel"] ?? DefaultModel,
                MaxTokens = ResolveMaxTokens(),
                System = system == null ? null : (MessageCreateParamsSystem)system,
                Messages = AnthropicRequestMapper.MapMessages(messages),
                Tools = AnthropicRequestMapper.MapTools(chatCompletion),
                Metadata = string.IsNullOrEmpty(chatCompletion.EndUserId)
                    ? null
                    : new Metadata { UserID = chatCompletion.EndUserId }
            }, cancellationToken: token);

            return new AIResponse
            {
                Text = AnthropicRequestMapper.MapText(response),
                FinishReason = response.StopReason?.ToString(),
                Refusal = AnthropicRequestMapper.MapRefusal(response),
                ToolCalls = AnthropicRequestMapper.MapToolCalls(response)
            };
        }
        catch (AnthropicApiException e)
        {
            throw new ClientResultException(e.Message, response: null, innerException: e);
        }
    }

    private int ResolveMaxTokens()
    {
        return int.TryParse(_config["AnthropicMaxTokens"], out var maxTokens) && maxTokens > 0
            ? maxTokens
            : DefaultMaxTokens;
    }
}
