namespace ErabliereApi.Services.Notifications;

/// <summary>
/// Service d'arrière-plan qui cherche périodiquement les capteurs muets
/// (voir <see cref="SilenceCapteurService.DetecterSilencesAsync"/>). Chaque instance de l'API
/// l'exécute : le jeton de concurrence de l'alerte empêche les messages en double.
/// </summary>
public class SurveillanceSilenceCapteursService : BackgroundService
{
    /// <summary>
    /// Intervalle entre deux vérifications. Un silence est signalé au plus un intervalle
    /// après l'expiration de son délai.
    /// </summary>
    public static TimeSpan Intervalle { get; set; } = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _temps;
    private readonly ILogger<SurveillanceSilenceCapteursService> _logger;

    /// <summary>
    /// Constructeur par initialisation
    /// </summary>
    public SurveillanceSilenceCapteursService(
        IServiceScopeFactory scopeFactory,
        TimeProvider temps,
        ILogger<SurveillanceSilenceCapteursService> logger)
    {
        _scopeFactory = scopeFactory;
        _temps = temps;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var minuterie = new PeriodicTimer(Intervalle, _temps);

            while (await minuterie.WaitForNextTickAsync(stoppingToken))
            {
                await VerifierAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Arrêt de l'application
        }
    }

    /// <summary>
    /// Une vérification. Une erreur est journalisée et la vérification suivante a lieu quand même.
    /// </summary>
    public async Task VerifierAsync(CancellationToken token)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();

            var service = scope.ServiceProvider.GetRequiredService<SilenceCapteurService>();

            await service.DetecterSilencesAsync(token);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _logger.LogError(e, "Une erreur imprévue est survenue lors de la recherche des capteurs muets.");
        }
    }
}
