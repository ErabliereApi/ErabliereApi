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
using ErabliereApi.Services.StripeIntegration;
using ErabliereApi.Services;
using ErabliereApi.Services.Nmap;
using ErabliereApi.Authorization.Customers;
using ErabliereApi.Middlewares;
using Microsoft.Extensions.Options;
using ErabliereApi.Services.Notifications;
using ErabliereApi.Services.AI;

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
            AddErabliereAPIForwardedHeaders(Configuration).
            AddErabliereAPIAuthentication(Configuration).
            AjouterSwagger(Configuration);

        // Hsts
        if (string.Equals(Configuration["USE_HSTS"], TrueString, InvariantCultureIgnoreCase))
        {
            services.AddHsts(options =>
            {
                options.Preload = true;
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(365); // Sets 1 year
            });
        }

        // Cors
        if (string.Equals(Configuration["USE_CORS"], TrueString, OrdinalIgnoreCase))
        {
            Console.WriteLine("CORS enabled. Services Added.");
            services.AddCors();
        }

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
                o.TimeSpanSendUsage = TimeSpan.FromSeconds(Convert.ToDouble(Configuration["StripeUsageReccord.TimeSpanSendUsageInSeconds"] ?? "300"));
                o.ThrowOnApiMissMatch =
                    Convert.ToBoolean(Configuration["Stripe.ThrowOnApiMissMatch"] ?? "true");
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
        services.AddTransient<IWeaterService, AccuWeatherService>();
        services.AddTransient<IWeaterService, GouvCAWeatherService>();

        // Nmap Service
        services.AddTransient<NmapService>();

        // IpInfo Middleware and Service
        services.AddIpInfoServices(Configuration);

        // AIService
        if (string.Equals(Configuration["PrimaryAIService"]?.Trim(), "Google", StringComparison.OrdinalIgnoreCase))
        {
            services.AddTransient<IAIService, GeminiAIService>();
        }
        else
        {
            services.AddTransient<IAIService, AzureOpenAIService>();
        }
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

        app.UseStatusCodePages(); // Formats status code errors as ProblemDetails
        app.UseExceptionHandler(new ExceptionHandlerOptions
        {
            SuppressDiagnosticsCallback = context => false
        }); // Formats unhandled exceptions as ProblemDetails

        if (string.Equals(Configuration["USE_HSTS"], TrueString, InvariantCultureIgnoreCase))
        {
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        if (Configuration.IsIpInfoEnabled())
        {
            app.UseMiddleware<IpInfoMiddleware>();
        }

        app.UseDefaultFiles();
        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = context =>
            {
                context.Context.Response.OnStarting(GenerateOnStaticFileResponseStartFunc(context.Context));
            }
        });

        var localizeOption = app.ApplicationServices.GetRequiredService<IOptions<RequestLocalizationOptions>>();
        app.UseRequestLocalization(localizeOption.Value);

        app.UseRouting();

        if (string.Equals(Configuration["USE_CORS"], TrueString, OrdinalIgnoreCase))
        {
            var corsHeaders = Configuration["CORS_HEADERS"]?.Split(',') ?? ["*"];
            var corsMethods = Configuration["CORS_METHODS"]?.Split(',') ?? ["*"];
            var corsOrigins = Configuration["CORS_ORIGINS"]?.Split(',') ?? ["*"];

            Console.WriteLine("CORS enabled. Middleware Added." +
                $"\n - Headers: {string.Join(", ", corsHeaders)}" +
                $"\n - Methods: {string.Join(", ", corsMethods)}" +
                $"\n - Origins: {string.Join(", ", corsOrigins)}");

            app.UseCors(option =>
            {
                option
                    .WithExposedHeaders("x-odatacount")
                    .WithExposedHeaders("x-odatanextlink")
                    .WithHeaders(corsHeaders)
                    .WithMethods(corsMethods)
                    .WithOrigins(corsOrigins);
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

        if (string.Equals(Configuration["USE_SECURITY_HEADERS"]?.Trim(), TrueString, InvariantCultureIgnoreCase))
        {
            app.Use(async (context, next) =>
            {
                context.Response.OnStarting(GenerateOnStaticFileResponseStartFunc(context));

                await next();
            });
        }

        app.UtiliserSwagger(Configuration);

        if (string.Equals(Configuration["USE_CONTENT_TYPE_FOR_SPA"], TrueString, InvariantCultureIgnoreCase))
        {
            app.Use(async (context, next) =>
            {
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers.Append("Content-Type", "text/html; charset=UTF-8");

                    return Task.CompletedTask;
                });

                await next();
            });
        }

        app.UseSpa(spa =>
        {

        });
    }

    private Func<Task> GenerateOnStaticFileResponseStartFunc(HttpContext context)
    {
        return () =>
        {
            if (string.Equals(Configuration["USE_SECURITY_HEADERS"]?.Trim(), TrueString, InvariantCultureIgnoreCase))
            {
                // WARNINGS & BEST PRACTICES:
                // - 'unsafe-inline' et 'unsafe-eval' doivent être ÉVITÉS en production
                //   sauf si absolument nécessaire. Ils réduisent considérablement l'efficacité de CSP.
                // - Préférez 'nonce' pour les scripts/styles inline (nécessite le rendu côté serveur de l'attribut nonce).
                // - Utilisez une politique CSP stricte qui n'autorise que les sources nécessaires.
                // - Revoyez et mettez à jour régulièrement votre CSP à mesure que votre application évolue.
                // - Utilisez 'Content-Security-Policy-Report-Only' en développement/test
                //   pour observer les violations sans bloquer le contenu.

                // 1. Générez un nonce unique pour les scripts/styles inline pour cette requête (FORTEMENT recommandé pour la sécurité)
                //    Ceci nécessite que votre code côté client ou votre moteur de template insère le même nonce dans les balises <script> et <style> inline.
                //    Si votre SPA ne génère pas de HTML côté serveur avec des nonces, vous devrez peut-être temporairement utiliser 'unsafe-inline' (à éviter).
                // string nonce = Guid.NewGuid().ToString("N");
                // context.Items["ScriptNonce"] = nonce; // Stockez-le pour une utilisation ultérieure dans les vues si vous rendez du HTML côté serveur.

                // 2. Ajoutez l'en-tête CSP à la réponse
                context.Response.Headers.Append("Content-Security-Policy", cspHeaderValue);
                // Pour les tests, utilisez "Content-Security-Policy-Report-Only" pour que les violations soient rapportées
                // mais pas bloquées (utile en développement pour affiner la politique).
                // context.Response.Headers.Add("Content-Security-Policy-Report-Only", cspHeaderValue);


                context.Response.Headers.Append("X-Frame-Options", "DENY");

                context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

                context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
            }

            return Task.CompletedTask;
        };
    }

    private static List<string> cspPolicy = new List<string>
    {
        // default-src: Politique par défaut pour la récupération de ressources (si non spécifié ailleurs).
        // 'self' signifie que seules les ressources de la même origine sont autorisées.
        "default-src 'self'",

        // script-src: Sources autorisées pour les scripts JavaScript.
        // - 'self': Scripts de votre propre domaine.
        // - 'unsafe-inline': AUTORISE TOUS LES SCRIPTS INLINE. À ÉVITER SI POSSIBLE.
        //   Mieux: Utilisez 'nonce-{random-value}' et appliquez l'attribut nonce à vos balises <script>.
        // - 'unsafe-eval': AUTORISE JS EVAL() ET SIMILAIRES. À ÉVITER SI POSSIBLE.
        //   Souvent nécessaire pour certains frameworks (ex: anciennes versions d'Angular, React DevTools).
        $"script-src 'self' 'unsafe-inline' 'unsafe-eval' https://api.mapbox.com", // Ajoutez votre nonce ici: 'nonce-{nonce}'

        // style-src: Sources autorisées pour les feuilles de style.
        // - 'self': Styles de votre propre domaine.
        // - 'unsafe-inline': AUTORISE TOUS LES STYLES INLINE. À ÉVITER SI POSSIBLE.
        //   Mieux: Utilisez 'nonce-{random-value}' et appliquez l'attribut nonce à vos balises <style>.
        $"style-src 'self' 'unsafe-inline' https://api.mapbox.com", // Ajoutez votre nonce ici: 'nonce-{nonce}'

        // img-src: Sources autorisées pour les images.
        // - 'self': Images de votre propre domaine.
        // - data:: Autorise les URIs de données (ex: images encodées en base64).
        "img-src 'self' data:",

        // font-src: Sources autorisées pour les polices.
        // - 'self': Polices de votre propre domaine.
        "font-src 'self'",

        // connect-src: Sources autorisées pour les requêtes (XHR, WebSockets, EventSource).
        // - 'self': Connexions à votre propre domaine (APIs).
        // - wss://your-websocket-domain.com: Exemple pour les connexions WebSocket.
        // - https://api.your-backend.com: Exemple pour une API backend distincte.
        "connect-src 'self' https://api.mapbox.com https://events.mapbox.com",

        // frame-src: Sources autorisées pour l'intégration de contenu dans des iframes.
        // - 'none': Désactive l'intégration de contenu dans des iframes.
        //   Si vous avez besoin d'iframes (ex: YouTube), listez les sources spécifiques (ex: https://www.youtube.com).
        "frame-src 'none'",

        // object-src: Sources autorisées pour les éléments <object>, <embed>, <applet> (Flash, Java applets).
        // - 'none': Généralement non nécessaire pour les applications web modernes.
        "object-src 'none'",

        // base-uri: Spécifie les URLs possibles pour l'élément <base>.
        "base-uri 'self'",

        // form-action: Restreint les URLs qui peuvent être utilisées comme cible d'une soumission de formulaire.
        "form-action 'self'",

        // frame-ancestors: Empêche le clickjacking en interdisant l'intégration de votre page dans des iframes/frames.
        // - 'none': Interdit toute intégration.
        "frame-ancestors 'none'",

        // worker-src
        "worker-src 'self' blob:"

        // report-uri / report-to: Pour le reporting des violations CSP.
        // - /csp-report: Envoyez les rapports de violation à ce point de terminaison sur votre serveur.
        //                Nécessite un gestionnaire côté serveur pour traiter ces rapports (ex: Serilog.Sinks.HttpCollector).
        // - 'report-to' est la norme plus récente et plus flexible, mais nécessite un en-tête Report-To séparé.
        // "report-uri /csp-report",
        // "report-to default" // Si vous définissez également un en-tête Report-To
    };

    private static string cspHeaderValue = string.Join("; ", cspPolicy);
}
