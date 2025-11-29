using ErabliereApi.ControllerFeatureProviders;
using ErabliereApi.Depot.Sql;
using ErabliereApi.Formaters;
using ErabliereApi.Middlewares;
using ErabliereApi.Services;
using ErabliereApi.Services.Emails;
using ErabliereApi.Services.IpInfo;
using ErabliereApi.Services.SMS;
using MailKit;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.OData;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MQTTnet.AspNetCore;
using StackExchange.Profiling;
using System.Data.Common;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static System.Boolean;
using static System.StringComparison;

namespace ErabliereApi.Extensions;

/// <summary>
/// Extension methods for configuring services in the Erabliere API.
/// </summary>
public static class ServiceCollectionExtension
{
    const string enUSCulture = "en-US";
    const string enCACulture = "en-CA";
    const string frCACulture = "fr-CA";

    /// <summary>
    /// Adds localization services to the service collection.
    /// This method configures the supported cultures and sets the default request culture.
    /// It also adds a custom request culture provider for initial request culture logic.
    /// </summary>
    /// <param name="services">The service collection to add localization services to.</param>
    public static IServiceCollection AddLocalisation(this IServiceCollection services)
    {
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

        return services;
    }

    /// <summary>
    /// Adds API controllers to the service collection with OData support and JSON options.
    /// This method configures the controllers to use OData features such as Select, Filter,
    /// OrderBy, and Expand, and sets JSON serialization options.
    /// It also conditionally adds a MiniProfilerAsyncLogger filter based on configuration.
    /// </summary>
    public static IServiceCollection AddErabliereApiControllers(this IServiceCollection services, IConfiguration config)
    {
        services.AddControllers(o =>
        {
            if (string.Equals(config["MiniProfiler.Enable"], TrueString, OrdinalIgnoreCase))
            {
                o.Filters.Add<MiniProfilerAsyncLogger>();
            }

            o.InputFormatters.Insert(0, new PlainTextInputFormatter());
        })
        .ConfigureApplicationPartManager(manager =>
        {
            // This code is used to scan for controller using the StripeIntegrationToggleFiltrer
            // which is going to control if the stripe controller must be enabled or disabled
            manager.FeatureProviders.Clear();
            manager.FeatureProviders.Add(new ErabliereApiControllerFeatureProvider(config));
        })
        .AddOData(o =>
        {
            o.Select().Filter().OrderBy().SetMaxTop(config.GetValue<int>("OData:MaxTop")).Expand().Count();
        })
        .AddJsonOptions(c =>
        {
            c.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
            c.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        });

        services.AddSingleton<ODataCountHeaderMiddleware>();

        return services;
    }

    /// <summary>
    /// Adds MQTT services to the service collection.
    /// This method checks the configuration to determine if MQTT is enabled,
    /// and if so, it configures the MQTT server with a default endpoint port.
    /// </summary>
    public static IServiceCollection AddMqtt(this IServiceCollection services, IConfiguration config)
    {
        if (config.UseMQTT())
        {
            services
                .AddHostedMqttServerWithServices(builder =>
                {
                    builder.WithDefaultEndpointPort(1883);
                })
                .AddMqttConnectionHandler()
                .AddConnections();
        }

        return services;
    }

    /// <summary>
    /// Add Database services to the service collection.
    /// This method checks the configuration to determine if SQL is used,
    /// and if so, it configures the DbContext to use SQL Server with a connection string.
    /// If SQL is not used, it configures the DbContext to use an in-memory database.
    /// It also supports MiniProfiler for SQL queries if enabled in the configuration.
    /// </summary>
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration config)
    {
        var useSql = config.UseSql();

        Console.WriteLine("Configuration de la base de donnée... USE_SQL: " + useSql);

        if (useSql)
        {
            services.AddDbContext<ErabliereDbContext>(options =>
            {
                var connectionString = config["SQL_CONNEXION_STRING"] ?? throw new InvalidOperationException("La variable d'environnement 'SQL_CONNEXION_STRING' à une valeur null.");

                if (string.Equals(config["MiniProlifer.EntityFramework.Enable"], TrueString, OrdinalIgnoreCase))
                {
                    DbConnection connection = new SqlConnection(connectionString);

                    connection = new StackExchange.Profiling.Data.ProfiledDbConnection(connection, MiniProfiler.Current);

                    options.UseSqlServer(connection, o => o.EnableRetryOnFailure());
                }
                else
                {
                    options.UseSqlServer(connectionString, o => o.EnableRetryOnFailure());
                }

                if (string.Equals(config["DbContextOptionsBuilder.EnableSensitiveDataLogging"], TrueString, OrdinalIgnoreCase))
                {
                    options.EnableSensitiveDataLogging();
                }

                if (string.Equals(config["LOG_SQL"], "Console", OrdinalIgnoreCase))
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

                if (string.Equals(config["DbContextOptionsBuilder.EnableSensitiveDataLogging"], TrueString, OrdinalIgnoreCase))
                {
                    options.EnableSensitiveDataLogging();
                }

            }, contextLifetime: ServiceLifetime.Singleton,
               optionsLifetime: ServiceLifetime.Transient);
        }

        return services;
    }

    /// <summary>
    /// Adds email services to the service collection.
    /// This method configures the email services based on the provided configuration.
    /// It registers two email service implementations: `ErabliereApiEmailService` and `MSGraphEmailService`.
    /// It also configures an SMTP client and a protocol logger for email communication.
    /// It reads email configuration from a specified path in the environment variable `EMAIL_CONFIG_PATH`.
    /// If the path is not set or the file cannot be read, it logs an error message and disables email functionality.
    /// </summary>
    public static IServiceCollection AddEmailServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddTransient<ErabliereApiEmailService>();
        services.AddTransient<MSGraphEmailService>();
        services.AddTransient<IEmailService>(sp =>
        {
            var o = sp.GetRequiredService<IOptions<EmailConfig>>().Value;

            if (o.UseMSGraphAPI.HasValue && o.UseMSGraphAPI.Value)
            {
                return sp.GetRequiredService<MSGraphEmailService>();
            }

            return sp.GetRequiredService<ErabliereApiEmailService>();
        });
        services.AddSingleton<ISmtpClient, SmtpClient>();
        services.AddSingleton<IProtocolLogger>(sp =>
        {
            if (config.IsDevelopment())
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
            var path = config["EMAIL_CONFIG_PATH"];

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

        return services;
    }

    /// <summary>
    /// Adds SMS services to the service collection.
    /// This method configures the SMS services based on the provided configuration.
    /// It registers the `TwilioSmsService` as the implementation of `ISmsService`.
    /// It reads SMS configuration from a specified path in the environment variable `SMS_CONFIG_PATH`.
    /// If the path is not set or the file cannot be read, it logs an error message and disables SMS functionality.
    /// </summary>
    public static IServiceCollection AddSMSServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddTransient<TwilioSmsService>();
        services.AddTransient<ISmsService, TwilioSmsService>();
        services.Configure<SMSConfig>(o =>
        {
            var path = config["SMS_CONFIG_PATH"];

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

        return services;
    }

    /// <summary>
    /// Adds IpInfo services and middleware to the service collection.
    /// This method checks the configuration to determine if the IpInfo middleware is enabled.
    /// If enabled, it registers the `IpInfoMiddleware` and configures an HTTP client for making requests to the IpInfo API.
    /// It sets the base address and authorization header for the HTTP client based on the configuration values.
    /// </summary>
    public static IServiceCollection AddIpInfoServices(this IServiceCollection services, IConfiguration config)
    {
        if (string.Equals(config["IpInfoApi:EnableMiddleware"], TrueString, OrdinalIgnoreCase))
        {
            services.AddMemoryCache();
            services.AddTransient<IpInfoMiddleware>();
            services.AddHttpClient("IpInfoClient", c =>
            {
                c.BaseAddress = new Uri(config.GetValue<string>("IpInfoApi:BaseUrl") ?? throw new InvalidOperationException("La variable d'environnement 'IpInfoApi:BaseUrl' à une valeur null."));

                var token = config["IpInfoApi:Token"];
                if (!string.IsNullOrWhiteSpace(token))
                {
                    c.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                }
            });
        }
        services.AddScoped<ImportIpInfoService>();

        return services;
    }

    /// <summary>
    /// Ajout des HTTP clients utilisés par l'api
    /// </summary>
    /// <param name="services"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static IServiceCollection AddHttpClients(this IServiceCollection services, IConfiguration config)
    {
        var emailImageObserverBaseUrl = config["EmailImageObserverUrl"];
        services.AddHttpClient("EmailImageObserver", c =>
        {
            c.BaseAddress = new Uri(emailImageObserverBaseUrl ?? "");
        });

        var hologramApiKey = config["Hologram_Token"];
        if (!string.IsNullOrWhiteSpace(hologramApiKey))
        {
            services.AddHttpClient("HologramClient", c =>
            {
                c.BaseAddress = new Uri(config.GetValue<string>("HologramBaseUrl") ?? throw new InvalidOperationException("La variable d'environnement 'HologramBaseUrl' à une valeur null."));
                var bearerValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"apikey:{hologramApiKey}"));
                c.DefaultRequestHeaders.Add("Authorization", $"Basic {bearerValue}");
            });
        }

        var ibmQuantum = config["IQP_API_TOKEN"];
        if (!string.IsNullOrWhiteSpace(ibmQuantum))
        {
            services.AddHttpClient("IbmQuantumClient", c =>
            {
                c.BaseAddress = new Uri(config.GetValue<string>("IbmQuantumBaseUrl") ?? throw new InvalidOperationException("La variable d'environnement 'IbmQuantumBaseUrl' à une valeur null."));
                c.DefaultRequestHeaders.Add("Authorization", $"Bearer {ibmQuantum}");
            });
        }

        var weatherBaseUrl = config["AccuWeatherBaseUrl"];
        if (!string.IsNullOrWhiteSpace(weatherBaseUrl))
        {
            services.AddHttpClient("AccuWeatherClient", c =>
            {
                c.BaseAddress = new Uri(weatherBaseUrl);
                var weatherApiKey = config["AccuWeatherApiKey"];
                if (!string.IsNullOrWhiteSpace(weatherApiKey))
                {
                    c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", weatherApiKey);
                }
            });
        }
        
        return services;
    }
}