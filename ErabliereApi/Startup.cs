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
using Microsoft.AspNetCore.OData;
using System.Text.Json.Serialization;
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
using ErabliereApi.ControllerFeatureProviders;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using MQTTnet.AspNetCore;
using System.Text;

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

    const string enUSCulture = "en-US";
    const string enCACulture = "en-CA";
    const string frCACulture = "fr-CA";

    /// <summary>
    /// Méthodes ConfigureServices
    /// </summary>
    public void ConfigureServices(IServiceCollection services)
    {
        // Localization
        services.AddLocalization(options => options.ResourcesPath = "Resources");

        services.Configure<RequestLocalizationOptions>(options =>
        {
            var supportedCultures = new[]
            {
                new CultureInfo(enUSCulture),
                new CultureInfo(enCACulture),
                new CultureInfo(frCACulture)
            };

            options.DefaultRequestCulture = new RequestCulture(frCACulture, frCACulture);
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;

            options.AddInitialRequestCultureProvider(new CustomRequestCultureProvider(async context =>
            {
                // My custom request culture logic
                return await Task.FromResult(new ProviderCultureResult(frCACulture));
            }));
        });

        // contrôleur
        services.AddControllers(o =>
        {
            if (string.Equals(Configuration["MiniProfiler.Enable"], TrueString, OrdinalIgnoreCase))
            {
                o.Filters.Add<MiniProfilerAsyncLogger>();
            }
        })
        .ConfigureApplicationPartManager(manager => 
        {
            // This code is used to scan for controller using the StripeIntegrationToggleFiltrer
            // which is going to control if the stripe controller must be enabled or disabled
            manager.FeatureProviders.Clear();
            manager.FeatureProviders.Add(new ErabliereApiControllerFeatureProvider(Configuration));
        })
        .AddOData(o =>
        {
            o.Select().Filter().OrderBy().SetMaxTop(100).Expand();
        })
        .AddJsonOptions(c =>
        {
            c.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
            c.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        });

        if (Configuration.UseMQTT())
        {
            services
                .AddHostedMqttServerWithServices(builder =>
                {
                    builder.WithDefaultEndpointPort(1883);
                })
                .AddMqttConnectionHandler()
                .AddConnections();
        }

        // Forwarded headers
        services.AddErabliereAPIForwardedHeaders(Configuration);

        // Authentication
        services.AddErabliereAPIAuthentication(Configuration);

        // Swagger
        services.AjouterSwagger(Configuration);

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
        if (string.Equals(Configuration["USE_SQL"], TrueString, OrdinalIgnoreCase))
        {
            services.AddDbContext<ErabliereDbContext>(options =>
            {
                var connectionString = Configuration["SQL_CONNEXION_STRING"] ?? throw new InvalidOperationException("La variable d'environnement 'SQL_CONNEXION_STRING' à une valeur null.");

                if (string.Equals(Configuration["MiniProlifer.EntityFramework.Enable"], TrueString, OrdinalIgnoreCase))
                {
                    DbConnection connection = new SqlConnection(connectionString);

                    connection = new StackExchange.Profiling.Data.ProfiledDbConnection(connection, MiniProfiler.Current);

                    options.UseSqlServer(connection, o => o.EnableRetryOnFailure());
                }
                else
                {
                    options.UseSqlServer(connectionString, o => o.EnableRetryOnFailure());
                }

                if (string.Equals(Configuration["LOG_SQL"], "Console", OrdinalIgnoreCase))
                {
                    options.LogTo(Console.WriteLine, LogLevel.Information);
                }
            });
        }
        else
        {
            services.AddDbContext<ErabliereDbContext>(options =>
            {
                options.UseInMemoryDatabase(nameof(ErabliereDbContext));

            }, contextLifetime: ServiceLifetime.Singleton, 
               optionsLifetime: ServiceLifetime.Transient);
        }

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

        // Email
        services.AddTransient<ErabliereApiEmailService>();
        services.AddTransient<MSGraphEmailService>();
        services.AddTransient<IEmailService>(sp =>
        {
            var o = sp.GetRequiredService<IOptions<EmailConfig>>().Value;

            if (o.UseMSGraphAPI == true)
            {
                return sp.GetRequiredService<MSGraphEmailService>();
            }

            return sp.GetRequiredService<ErabliereApiEmailService>();
        });
        services.AddSingleton<ISmtpClient, SmtpClient>();
        services.AddSingleton<IProtocolLogger>(sp =>
        {
            if (Configuration.IsDevelopment())
            {
                return new ProtocolLogger(Console.OpenStandardOutput());
            }
            else
            {
                return new ProtocolLogger(Stream.Null);
            }
        });
        services.AddSingleton<SmtpClient>(sp =>
        {
           return new SmtpClient(sp.GetRequiredService<IProtocolLogger>());
        });
        services.Configure<EmailConfig>(o =>
        {
            var path = Configuration["EMAIL_CONFIG_PATH"];

            if (string.IsNullOrWhiteSpace(path))
            {
                Console.WriteLine("La variable d'environment 'EMAIL_CONFIG_PATH' ne possédant pas de valeur, les configurations de courriel ne seront pas désérialisé.");
            }
            else
            {
                try
                {
                    var v = File.ReadAllText(path);

                    var deserializedConfig = JsonSerializer.Deserialize<EmailConfig>(v);

                    if (deserializedConfig != null)
                    {
                        o.Sender = deserializedConfig.Sender;
                        o.Email = deserializedConfig.Email;
                        o.Password = deserializedConfig.Password;
                        o.TenantId = deserializedConfig.TenantId;
                        o.SmtpServer = deserializedConfig.SmtpServer;
                        o.SmtpPort = deserializedConfig.SmtpPort;
                        o.UseMSGraphAPI = deserializedConfig.UseMSGraphAPI;
                    }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("Erreur en désérialisant les configurations de l'email. La fonctionnalité des alertes ne pourra pas être utilisé.");
                    Console.Error.WriteLine(e.Message);
                    Console.Error.WriteLine(e.StackTrace);
                }
            }
        });

        // SMS
        services.AddTransient<TwilioSmsService>();
        services.AddTransient<ISmsService, TwilioSmsService>();
        services.Configure<SMSConfig>(o =>
        {
            var path = Configuration["SMS_CONFIG_PATH"];

            if (string.IsNullOrWhiteSpace(path))
            {
                Console.WriteLine("La variable d'environment 'SMS_CONFIG_PATH' ne possédant pas de valeur, les configurations de SMS ne seront pas désérialisé.");
            }
            else
            {
                try
                {
                    var v = File.ReadAllText(path);

                    var deserializedConfig = JsonSerializer.Deserialize<SMSConfig>(v);

                    if (deserializedConfig != null)
                    {
                        o.Numero = deserializedConfig.Numero;
                        o.AccountSid = deserializedConfig.AccountSid;
                        o.AuthToken = deserializedConfig.AuthToken;
                    }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("Erreur en désérialisant les configurations du SMS. La fonctionnalité des alertes ne pourra pas être utilisé.");
                    Console.Error.WriteLine(e.Message);
                    Console.Error.WriteLine(e.StackTrace);
                }
            }
        });


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
