using ErabliereApi.Services.AI;
using ErabliereApi.Services.AI.Tools;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Collections.Generic;

namespace ErabliereApi.Integration.Test.ApplicationFactory;

/// <summary>
/// Une API dont ErabliereAI parle à un fournisseur scénarisé plutôt qu'à un vrai
/// modèle, mais dont les outils lisent les données par le vrai pipeline HTTP.
/// </summary>
/// <remarks>
/// C'est le point de tout l'exercice : les appels d'outils repassent par le
/// <c>TestServer</c>, donc par l'authentification, l'autorisation et les filtres
/// de propriété réels. Un test qui montre qu'un utilisateur n'atteint pas les
/// données d'un autre à travers la conversation ne prouve quelque chose que parce
/// qu'aucune de ces couches n'est simulée ici.
/// </remarks>
public class ErabliereAIApplicationFactory<TStartup> : StripeEnabledApplicationFactory<TStartup> where TStartup : class
{
    /// <summary>
    /// Le fournisseur scénarisé, à alimenter avant d'envoyer un prompt.
    /// </summary>
    public ScriptedAIService AI { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        var config = new Dictionary<string, string?>
        {
            // ErabliereApiControllerFeatureProvider n'enregistre ErabliereAIController
            // que si une clé de fournisseur est configurée. Sans cette valeur, la
            // route n'existe pas et les tests passent ou échouent selon que la machine
            // qui les exécute a des secrets utilisateur — ce qui n'est pas un test.
            // Aucune clé n'est jamais utilisée : IAIService est remplacé plus bas.
            { "AzureOpenAIUri", "https://exemple.invalid/openai" },
            { "LLMDefaultTemperature", "1" },
            // Les outils rappellent l'API par le gestionnaire du TestServer, dont
            // l'hôte est localhost.
            { "ErabliereAI:Tools:ApiBaseUrl", "http://localhost" },
            { "ErabliereAI:Tools:Enabled", "true" },
            { "Mcp:PlanGating:Enabled", "false" },
            // Un délai court : un test qui bloque doit échouer vite, pas au bout de 20 secondes.
            { "ErabliereAI:Tools:ToolTimeout", "00:00:05" }
        };

        builder.ConfigureAppConfiguration(app => app.AddInMemoryCollection(config));

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IAIService>();
            services.AddSingleton<IAIService>(AI);

            // Le client nommé des outils doit entrer dans le TestServer plutôt que
            // d'ouvrir une vraie connexion réseau. Le gestionnaire est résolu à la
            // création du client, donc après la construction de l'hôte.
            services.AddHttpClient(ErabliereAiToolOptions.HttpClientName)
                    .ConfigurePrimaryHttpMessageHandler(_ => Server.CreateHandler());
        });
    }
}
