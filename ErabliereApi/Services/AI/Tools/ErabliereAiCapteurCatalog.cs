using ErabliereAPI.Proxy;
using ErabliereApi.Extensions;
using ErabliereApi.Mcp.Tools;
using Microsoft.Extensions.Options;

namespace ErabliereApi.Services.AI.Tools;

/// <summary>
/// Default <see cref="IErabliereAiCapteurCatalog" />, reading through the very tool
/// the model would have called.
/// </summary>
/// <remarks>
/// <see cref="CapteurTools.ListCapteursAsync" /> is invoked directly rather than
/// through <see cref="IErabliereAiToolset" />: the result is wanted as objects, not
/// as the json a model reads, and nothing here goes into the tool transcript. The
/// authorization is unchanged either way — the proxy is the request scoped one,
/// whose http client forwards the credentials of the caller, so a maple grove the
/// user does not own is refused by the API exactly as it would be for a tool call.
/// </remarks>
public class ErabliereAiCapteurCatalog : IErabliereAiCapteurCatalog
{
    /// <summary>
    /// How many sensors are named in the prompt at most.
    /// </summary>
    /// <remarks>
    /// The point is to spare the model a guess, not to move its whole installation
    /// into the system prompt. Past this, listing them costs more context than the
    /// tool call it saves, so the model is left to call <c>list_capteurs</c>.
    /// </remarks>
    public const int MaxCapteurs = 40;

    private readonly IErabliereAPIProxy _proxy;
    private readonly IOptions<ErabliereAiToolOptions> _options;
    private readonly ILogger<ErabliereAiCapteurCatalog> _logger;

    /// <summary>
    /// Constructeur par initialisation
    /// </summary>
    public ErabliereAiCapteurCatalog(
        IErabliereAPIProxy proxy,
        IOptions<ErabliereAiToolOptions> options,
        ILogger<ErabliereAiCapteurCatalog> logger)
    {
        _proxy = proxy;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ErabliereAiCapteur>> ReadAsync(Guid erabliereId, CancellationToken token)
    {
        if (erabliereId == Guid.Empty)
        {
            return [];
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(_options.Value.ToolTimeout);

        try
        {
            var response = await CapteurTools.ListCapteursAsync(
                _proxy,
                erabliereId.ToString(),
                nameContains: null,
                top: MaxCapteurs,
                timeout.Token);

            return [.. response.Data
                .Where(capteur => capteur.Id is not null && !string.IsNullOrWhiteSpace(capteur.Nom))
                .Select(capteur => new ErabliereAiCapteur(
                    capteur.Id!.Value,
                    capteur.Nom!.Trim(),
                    capteur.Symbole,
                    capteur.Online))];
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // The user gave up on the whole request; there is nobody left to answer.
            throw;
        }
        catch (Exception exception)
        {
            // Nothing here is required to answer: the model still has list_capteurs,
            // and the prompt still tells it to list before it filters. A maple grove
            // the caller does not own lands here too, which is the correct outcome —
            // the prompt simply names no sensor.
            _logger.LogWarning(exception,
                "ErabliereAI could not pre-read the sensors of the maple grove {Erabliere}; the model will list them itself.",
                erabliereId.ToString().Sanatize());

            return [];
        }
    }
}
