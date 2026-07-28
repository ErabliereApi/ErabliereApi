using ErabliereApi.Depot.Sql;
using ErabliereApi.Services.AI.Tools;
using ErabliereApi.Services.IpInfo;
using Microsoft.EntityFrameworkCore;
using static System.Boolean;
using static System.StringComparison;

namespace ErabliereApi.Extensions;

/// <summary>
/// Extensions for the ApplicationBuilder
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Migrate the database at startup
    /// </summary>
    public static IApplicationBuilder MigrateDatabase(this IApplicationBuilder app, IConfiguration config, IServiceProvider serviceProvider)
    {
        try
        {
            if (config.UseSql() &&
            string.Equals(config["SQL_USE_STARTUP_MIGRATION"], TrueString, OrdinalIgnoreCase))
            {
                var database = serviceProvider.GetRequiredService<ErabliereDbContext>();

                var defaultMigrationTimeout = database.Database.GetCommandTimeout();

                Console.WriteLine("Default migration timeout: " + defaultMigrationTimeout);

                var migrationTimeout = config["SQL_STARTUP_MIGRATION_TIMEOUT"];

                if (migrationTimeout != null)
                {
                    database.Database.SetCommandTimeout(int.Parse(migrationTimeout));

                    Console.WriteLine("Migration timeout: " + migrationTimeout);
                }

                database.Database.Migrate();
            }

            if (config.IsIpInfoEnabled())
            {
                ImportIPInfoDatabase(config, serviceProvider);
            }
        }
        catch (Exception e)
        {
            throw new InvalidOperationException(
                $"Erreur lors de la migration initial à {config["SQL_CONNEXION_STRING"]}", e);
        }

        return app;
    }

    private static void ImportIPInfoDatabase(IConfiguration config, IServiceProvider serviceProvider)
    {
        var ipInfoService = serviceProvider.GetRequiredService<ImportIpInfoService>();

        var filePath = config["IpInfoApi:DBFilePath"];

        if (!string.IsNullOrWhiteSpace(filePath))
        {
            Console.WriteLine("Importing IP info database from file: " + filePath);

            FileStream? stream = null;

            try
            {
                stream = File.OpenRead(filePath);

                ipInfoService.ImportIpInfoAsync(stream, importIfNotEmpty: false, CancellationToken.None).Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error importing IP info database: " + ex.Message);
            }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                    stream.Dispose();
                }
            }
        }
        else
        {
            Console.WriteLine("No file path provided for IP info database import");
        }
    }

    /// <summary>
    /// Add a semaphore to limit concurrent access to the in-memory database
    /// </summary>
    /// <param name="app"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    public static IApplicationBuilder AddSemaphoreOnInMemoryDatabase(this IApplicationBuilder app, IConfiguration config)
    {
        if (!config.UseSql())
        {
            Console.WriteLine("Using in-memory database, semaphore added");

            var semaphore = new SemaphoreSlim(1, 1);
            var loopback = app.ApplicationServices.GetRequiredService<LoopbackRequestMarker>();

            app.Use(async (context, next) =>
            {
                // Une requête imbriquée — ErabliereAI rappelle l'API pour exécuter ses
                // outils avec les identifiants de l'appelant — est servie pendant que
                // la requête externe l'attend. La faire attendre le sémaphore que
                // l'externe détient bloquerait les deux jusqu'à l'expiration du délai.
                // La laisser passer reste sûr : l'externe n'utilise pas la base tant
                // qu'elle attend l'interne.
                if (loopback.IsLoopback(context.Request))
                {
                    await next();
                    return;
                }

                var acquis = false;

                try
                {
                    await semaphore.WaitAsync(context.RequestAborted);

                    acquis = true;

                    await next();
                }
                catch (OperationCanceledException e)
                {
                    var logger = context.RequestServices.GetRequiredService<ILogger<Startup>>();
                    logger.LogWarning(e, "Operation was canceled while waiting for semaphore");
                }
                finally
                {
                    // Seulement si l'attente a réussi : relâcher un sémaphore jamais
                    // acquis lève SemaphoreFullException et casse la requête suivante.
                    if (acquis)
                    {
                        semaphore.Release();
                    }
                }
            });
        }

        return app;
    }
}
