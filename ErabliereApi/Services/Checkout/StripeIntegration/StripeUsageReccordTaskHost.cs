using ErabliereApi.Extensions;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Billing;

namespace ErabliereApi.Services.StripeIntegration;

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
                if (_task != null && _task.Status != TaskStatus.WaitingForActivation)
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


    /// <summary>
    /// Clé de la valeur dans le payload.
    /// Représente la quantité d'utilisation.
    /// Soit le nombre d'API Key request par clé d'api.
    /// </summary>
    private const string PayloadValueKey = "value";

    private async Task EnvoyerUtilisationAsync()
    {
        var skip = _config.GetValue<string>("StripeUsageReccord.SkipRecord");

        if (skip == "true")
        {
            Console.WriteLine("Envoie de l'utilisation à Stripe ignorée.");
            return;
        }

        using var scope = Services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<UsageContext>();
        
        Console.WriteLine($"Envoie de {context.Usages.Count} utilisations à Stripe... ");

        var usageSummary = new Dictionary<string, MeterEventCreateOptions>(context.Usages.Count);

        while (context.Usages.TryDequeue(out var usageReccorded))
        {
            if (usageReccorded.SubscriptionId == null)
            {
                continue;
            }

            Console.WriteLine($"Utilisation dans la file : {usageReccorded.SubscriptionId} - {usageReccorded.Quantite}");

            if (usageSummary.TryGetValue(usageReccorded.SubscriptionId, out var usage))
            {
                var actuel = int.Parse(usage.Payload[PayloadValueKey] ?? "0");
                actuel += usageReccorded.Quantite;
                usage.Payload[PayloadValueKey] = actuel.ToString();
            }
            else
            {
                var createOptions = new MeterEventCreateOptions
                {
                    EventName = "api_key_request",
                    Payload = new Dictionary<string, string>
                    {
                        { PayloadValueKey, usageReccorded.Quantite.ToString() },
                        { "stripe_customer_id", usageReccorded.CustomerId }
                    },
                    Identifier = Guid.NewGuid().ToString(),
                    Timestamp = DateTime.UtcNow
                };

                usageSummary.Add(usageReccorded.SubscriptionId, createOptions);
            }
        }

        var options = scope.ServiceProvider.GetRequiredService<IOptions<StripeOptions>>();

        StripeConfiguration.ApiKey = options.Value.ApiKey;

        foreach (var usageReccord in usageSummary)
        {
            Console.WriteLine($"Envoie de l'utilisation pour la souscription {usageReccord.Key} : {usageReccord.Value.Payload[PayloadValueKey]}");
            try
            {
                var service = new MeterEventService();
                var meterEvent = await service.CreateAsync(usageReccord.Value);

                // Log the result
                Console.WriteLine($"Utilisation envoyée : {meterEvent}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'envoie de l'utilisation pour la souscription {usageReccord.Key} : {ex.Message}");
            }
            
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