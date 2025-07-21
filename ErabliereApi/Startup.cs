using ErabliereApi.Donnees.AutoMapper;
using ErabliereApi.Depot.Sql;
using Microsoft.EntityFrameworkCore;
using static System.Boolean;
using static System.StringComparison;
using ErabliereApi.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Logging;
using ErabliereApi.Extensions;
using StackExchange.Profiling;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using StackExchange.Profiling.SqlFormatters;
using System.Text.Json;
using Prometheus;
using ErabliereApi.HealthCheck;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ErabliereApi.StripeIntegration;
using ErabliereApi.Services;
using ErabliereApi.Authorization.Customers;
using ErabliereApi.Middlewares;
using ErabliereApi.Models;
using ErabliereApi.Donnees;
using MailKit.Net.Smtp;
using MailKit;
using Microsoft.Extensions.Options;
using ErabliereApi.Services.Emails;
using ErabliereApi.Services.SMS;
using System.Text;
using ErabliereApi.Services.Notifications;
using MQTTnet.AspNetCore;

namespace ErabliereApi;

/// <summary>
/// Classe Startup de l'api
/// </summary>
public class Startup
{
    /// <summary>
    /// Configuration of the app. Use when AzureAD authentication method is used.
    /// </summary>
    public IConfiguration Configuration { get; }

    /// <summary>
    /// Constructor of the startup class with the configuration object in parameter
    /// </summary>
    /// <param name="configuration"></param>
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    /// <summary>
    /// Méthodes ConfigureServices
    /// </summary>
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddLocalisation().
            AddErabliereApiControllers(Configuration).
            AddMqtt(Configuration).
            AddErabliereAPIForwardedHeaders(Configuration).
            AddErabliereAPIAuthentication(Configuration).
            AjouterSwagger(Configuration);

        // Cors
        if (string.Equals(Configuration["USE_CORS"], TrueString, OrdinalIgnoreCase))
        {
            Console.WriteLine("CORS enabled. Services Added.");
            services.AddCors();
        }

        // Automapper
        services.AjouterAutoMapperErabliereApiDonnee(config => 
            {
                StripeIntegration.AutoMapperExtension.AddCustomersApiKeyMappings(config);

                config.CreateMap<PostNoteMultipart, Note>()
                  .ForMember(d => d.File, o => o.Ignore())
                  .ReverseMap()
                  .ForMember(d => d.File, o => o.Ignore());
            }
        );

        // HttpClient
        var emailImageObserverBaseUrl = Configuration["EmailImageObserverUrl"];
        if (!string.IsNullOrWhiteSpace(emailImageObserverBaseUrl)) 
        {
            services.AddHttpClient("EmailImageObserver", c =>
            {
                c.BaseAddress = new Uri(emailImageObserverBaseUrl);
            });
        }

        var hologramApiKey = Configuration["Hologram_Token"];
        if (!string.IsNullOrWhiteSpace(hologramApiKey)) 
        {
            services.AddHttpClient("HologramClient", c =>
            {
                c.BaseAddress = new Uri(Configuration.GetValue<string>("HologramBaseUrl") ?? throw new InvalidOperationException("La variable d'environnement 'HologramBaseUrl' à une valeur null."));
                var bearerValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"apikey:{hologramApiKey}"));
                c.DefaultRequestHeaders.Add("Authorization", $"Basic {bearerValue}");
            });
        }

        var ibmQuantum = Configuration["IQP_API_TOKEN"];
        if (!string.IsNullOrWhiteSpace(ibmQuantum)) 
        {
            services.AddHttpClient("IbmQuantumClient", c =>
            {
                c.BaseAddress = new Uri(Configuration.GetValue<string>("IbmQuantumBaseUrl") ?? throw new InvalidOperationException("La variable d'environnement 'IbmQuantumBaseUrl' à une valeur null."));
                c.DefaultRequestHeaders.Add("Authorization", $"Bearer {ibmQuantum}");
            });
        }

        // Database
        services.AddDatabase(Configuration);

        // HealthCheck
        services.AddHealthChecks()
                .AddCheck<MemoryUsageCheck>(nameof(MemoryUsageCheck), HealthStatus.Degraded, new[]
                {
                    "live",
                    "memory"
                });

        // MiniProfiler
        if (string.Equals(Configuration["MiniProfiler.Enable"], TrueString, OrdinalIgnoreCase))
        {
            var profilerBuilder = services.AddMiniProfiler(o =>
            {
                if (string.Equals(Configuration["MiniProlifer.EntityFramework.Enable"], TrueString, OrdinalIgnoreCase))
                {
                    o.SqlFormatter = new SqlServerFormatter();
                }
            });

            if (string.Equals(Configuration["MiniProlifer.EntityFramework.Enable"], TrueString, OrdinalIgnoreCase))
            {
                profilerBuilder.AddEntityFramework();
            }
        }

        // Prometheus
        services.AddSingleton<CollectorRegistry>(Metrics.DefaultRegistry);

        // Stripe
        services.AddScoped<ApiKeyAuthorizationContext>();
        if (Configuration.StripeIsEnabled())
        {
            services.Configure<StripeOptions>(o =>
            {
                o.ApiKey = Configuration["Stripe.ApiKey"];
                o.SuccessUrl = Configuration["Stripe.SuccessUrl"];
                o.CancelUrl = Configuration["Stripe.CancelUrl"];
                o.BasePlanPriceId = Configuration["Stripe.BasePlanPriceId"];
                o.WebhookSecret = Configuration["Stripe.WebhookSecret"];
                o.WebhookSiginSecret = Configuration["Stripe.WebhookSiginSecret"];
            });

            services.AddTransient<ICheckoutService, StripeCheckoutService>()
                    .AddTransient<IApiKeyService, ApiApiKeyService>()
                    .AddTransient<ApiKeyMiddleware>();

            // Authorization
            services.AddSingleton<IAuthorizationHandler, ApiKeyAuthrizationHandler>();

            // Context and usage reccorder
            services.AddSingleton<UsageContext>();
        }

        // Notifications
        services.AddEmailServices(Configuration)
                .AddSMSServices(Configuration)
                .AddTransient<NotificationService>();


        // Distributed cache
        if (string.Equals(Configuration["USE_DISTRIBUTED_CACHE"], TrueString, OrdinalIgnoreCase))
        {
            Console.WriteLine("Distributed cache enabled. Using Redis " + Configuration["REDIS_CONNEXION_STRING"]);
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = Configuration["REDIS_CONNEXION_STRING"];
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        // ChaosEngineering
        if (Configuration.IsChaosEngineeringEnabled()) 
        {
            services.AddSingleton<ChaosEngineeringMiddleware>();
        }

        // Weather Service
        services.AddTransient<WeatherService>();
    }

    /// <summary>
    /// Configure
    /// </summary>
    public void Configure(
        IApplicationBuilder app, 
        IWebHostEnvironment env, 
        IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Startup>>();

        if (string.Equals(Configuration["USE_SQL"], TrueString, OrdinalIgnoreCase) &&
            string.Equals(Configuration["SQL_USE_STARTUP_MIGRATION"], TrueString, OrdinalIgnoreCase))
        {
            var database = serviceProvider.GetRequiredService<ErabliereDbContext>();

            var defaultMigrationTimeout = database.Database.GetCommandTimeout();

            Console.WriteLine("Default migration timeout: " + defaultMigrationTimeout);

            var migrationTimeout = Configuration["SQL_STARTUP_MIGRATION_TIMEOUT"];

            if (migrationTimeout != null)
            {
                database.Database.SetCommandTimeout(int.Parse(migrationTimeout));

                Console.WriteLine("Migration timeout: " + migrationTimeout);
            }

            database.Database.Migrate();
        }

        if (env.IsDevelopment())
        {
            IdentityModelEventSource.ShowPII = true;
            app.UseDeveloperExceptionPage();
        }

        if (string.Equals(Configuration["MiniProfiler.Enable"], TrueString, OrdinalIgnoreCase))
        {
            app.UseMiniProfiler();
        }

        app.UseErabliereAPIForwardedHeadersRules(logger, Configuration);

        app.UseDefaultFiles();
        app.UseStaticFiles();

        var localizeOption = app.ApplicationServices.GetRequiredService<IOptions<RequestLocalizationOptions>>();
        app.UseRequestLocalization(localizeOption.Value);

        app.UseRouting();

        if (string.Equals(Configuration["USE_CORS"], TrueString, OrdinalIgnoreCase))
        {
            Console.WriteLine("CORS enabled. Middleware Added.");

            app.UseCors(option =>
            {
                option.WithHeaders(Configuration["CORS_HEADERS"]?.Split(',') ?? ["*"]);
                option.WithMethods(Configuration["CORS_METHODS"]?.Split(',') ?? ["*"]);
                option.WithOrigins(Configuration["CORS_ORIGINS"]?.Split(',') ?? ["*"]);
            });
        }

        if (Configuration.StripeIsEnabled())
        {
            app.UseMiddleware<ApiKeyMiddleware>();
        }

        if (Configuration.IsAuthEnabled())
        {
            app.UseAuthentication();
        }
        app.UseAuthorization();

        app.UseMetricServer(registry: serviceProvider.GetRequiredService<CollectorRegistry>());
        app.UseHttpMetrics();

        if (Configuration.IsAuthEnabled())
        {
            app.UseMiddleware<EnsureCustomerExist>();
        }

        if (Configuration.IsChaosEngineeringEnabled())
        {
            if (env.IsProduction()) 
            {
                logger.LogWarning("Chaos engineering is enabled in production. This is not recommended.");
            }
            logger.LogInformation("Chaos engineering is enabled with a probability of {0}%", Configuration["ChaosEngineeringPercent"]);
            
            app.UseMiddleware<ChaosEngineeringMiddleware>();
        }

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapHealthChecks("/health", new HealthCheckOptions
            {
                Predicate = r => !r.Tags.Contains("live")
            });
            endpoints.MapHealthChecks("/live", new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
        });

        if (Configuration.UseMQTT())
        {
            app.UseMqttServer(server => 
            {
                server.ClientConnectedAsync += async (e) =>
                {
                    Console.WriteLine($"Client {e.ClientId} connected");
                    await Task.CompletedTask;
                };

                server.ClientDisconnectedAsync += async (e) =>
                {
                    Console.WriteLine($"Client {e.ClientId} disconnected");
                    await Task.CompletedTask;
                };
            });
        }

        app.UtiliserSwagger(Configuration);

        app.UseSpa(spa =>
        {
        
        });
    }
}
