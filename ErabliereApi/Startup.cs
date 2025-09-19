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
using StackExchange.Profiling.SqlFormatters;
using Prometheus;
using ErabliereApi.HealthCheck;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ErabliereApi.StripeIntegration;
using ErabliereApi.Services;
using ErabliereApi.Services.Nmap;
using ErabliereApi.Authorization.Customers;
using ErabliereApi.Middlewares;
using ErabliereApi.Models;
using ErabliereApi.Donnees;
using Microsoft.Extensions.Options;
using System.Text;
using ErabliereApi.Services.Notifications;
using MQTTnet.AspNetCore;
using System.Net.Http.Headers;

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
        services.AddHttpClients(Configuration);

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
        services.AddSingleton(Metrics.DefaultRegistry);

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

        // Nmap Service
        services.AddTransient<NmapService>();

        // IpInfo Middleware and Service
        services.AddIpInfoServices(Configuration);
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

        app.MigrateDatabase(Configuration, serviceProvider);

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

        if (Configuration.IsIpInfoEnabled())
        {
            app.UseMiddleware<IpInfoMiddleware>();
        }

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
                option.WithExposedHeaders("x-odatacount");
                option.WithHeaders(Configuration["CORS_HEADERS"]?.Split(',') ?? ["*"]);
                option.WithMethods(Configuration["CORS_METHODS"]?.Split(',') ?? ["*"]);
                option.WithOrigins(Configuration["CORS_ORIGINS"]?.Split(',') ?? ["*"]);
            });
        }

        app.AddSemaphoreOnInMemoryDatabase(Configuration);

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

        app.UseMiddleware<ODataCountHeaderMiddleware>();

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
