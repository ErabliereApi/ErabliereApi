using ErabliereApi.Extensions;
using Microsoft.Extensions.Options;
using Stripe;

namespace ErabliereApi.StripeIntegration;

/// <summary>
/// Une class contenant une méthode d'extension pour décorer l'IHost
/// </summary>
public static class StripeUsageReccordTaskHostExtensions
{
    /// <summary>
    /// Décorateur permettant d'ajouter une tâche de d'envoie de l'utilisation
    /// lorsque Stripe est activé avec la variable d'environnement Stripe.ApiKey
    /// </summary>
    /// <param name="host"></param>
    /// <returns></returns>
    public static IHost UseStripeUsageReccordTask(this IHost host)
    {
        var config = host.Services.GetRequiredService<IConfiguration>();

        if (config.StripeIsEnabled())
        {
            return new StripeUsageReccordTaskHost(host, config);
        }

        return host;
    }
}

/// <summary>
/// Décorateur de IHost ajoutant une tâche en arrière pour envoyer l'utilisation à Stripe.
/// </summary>
public class StripeUsageReccordTaskHost : IHost
{
    private readonly IHost _host;
    private readonly IConfiguration _config;
    private Task? _task;
    private bool _disposed = false;

    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="StripeUsageReccordTaskHost"/>.
    /// </summary>
    public StripeUsageReccordTaskHost(IHost host, IConfiguration config)
    {
        _host = host;
        _config = config;
    }

    /// <summary>
    /// Services
    /// </summary>
    public IServiceProvider Services => _host.Services;

    /// <summary>
    /// Dispose
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Dispose pattern implementation
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                if (_task != null && _task.Status != TaskStatus.Created)
                {
                    _task.Dispose();
                }
                
                _host.Dispose();
            }
            _disposed = true;
        }
    }

    /// <summary>
    /// Finalizer
    /// </summary>
    ~StripeUsageReccordTaskHost()
    {
        Dispose(false);
    }

    /// <summary>
    /// Démarre l'host
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _task = Task.Run(async () => 
        {
            var options = Services.GetRequiredService<IOptions<StripeOptions>>();

            while (!cancellationToken.IsCancellationRequested) 
            {
                await Task.Delay(options.Value.TimeSpanSendUsage, cancellationToken);

                if (!cancellationToken.IsCancellationRequested)
                {
                    await EnvoyerUtilisationAsync();
                }
            }
        });

        return _host.StartAsync(cancellationToken);
    }

    private async Task EnvoyerUtilisationAsync()
    {
        var skip = _config.GetValue<string>("StripeUsageReccord.SkipRecord");

        if (skip == "true")
        {
            return;
        }

        using var scope = Services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<UsageContext>();

        var usageSummary = new Dictionary<string, SubscriptionItemCreateOptions>(context.Usages.Count);

        while (context.Usages.TryDequeue(out var usageReccorded))
        {
            if (usageReccorded.SubscriptionId == null)
            {
                continue;
            }

            if (usageSummary.TryGetValue(usageReccorded.SubscriptionId, out var usage))
            {
                usage.Quantity += usageReccorded.Quantite;
            }
            else
            {
                var createOptions = new SubscriptionItemCreateOptions
                {
                    Subscription = usageReccorded.SubscriptionId,
                    Quantity = usageReccorded.Quantite
                };

                if (createOptions.Metadata == null)
                {
                    createOptions.Metadata = new Dictionary<string, string>();
                }

                createOptions.Metadata.Add("Timestamp", DateTimeOffset.UtcNow.ToString());

                usageSummary.Add(usageReccorded.SubscriptionId, createOptions);
            }
        }

        var options = scope.ServiceProvider.GetRequiredService<IOptions<StripeOptions>>();

        StripeConfiguration.ApiKey = options.Value.ApiKey;

        foreach (var usageReccord in usageSummary)
        {
            var service = new SubscriptionItemService();
            await service.CreateAsync(usageReccord.Value);
        }
    }

    /// <summary>
    /// Arrête l'host
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await EnvoyerUtilisationAsync();

        await _host.StopAsync(cancellationToken);
    }
}