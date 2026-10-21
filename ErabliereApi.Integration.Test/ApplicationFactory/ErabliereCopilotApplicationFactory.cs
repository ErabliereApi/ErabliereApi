using ErabliereApi.Services.AI.CopilotKit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ErabliereApi.Integration.Test.ApplicationFactory;

/// <summary>
/// L'API avec l'endpoint AG-UI de CopilotKit activé, dont le modèle est scénarisé
/// mais dont les outils lisent les données par le vrai pipeline HTTP, comme dans
/// <see cref="ErabliereAIApplicationFactory{TStartup}" />.
/// </summary>
public class ErabliereCopilotApplicationFactory<TStartup> : ErabliereAIApplicationFactory<TStartup> where TStartup : class
{
    /// <summary>
    /// Le modèle scénarisé, à alimenter avant d'envoyer une requête AG-UI.
    /// </summary>
    public ScriptedChatClient Model { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.UseSetting(ErabliereCopilotExtension.EnabledKey, "true");

        builder.ConfigureServices(services =>
        {
            services.RemoveAllKeyed<IChatClient>(ErabliereCopilotExtension.AgentName);
            services.AddKeyedSingleton<IChatClient>(ErabliereCopilotExtension.AgentName, Model);
        });
    }
}
