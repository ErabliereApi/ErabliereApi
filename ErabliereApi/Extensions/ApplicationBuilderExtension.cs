using ErabliereApi.Depot.Sql;
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

            app.Use(async (context, next) =>
            {
                try
                {
                    await semaphore.WaitAsync(context.RequestAborted);

                    await next();
                }
                catch (OperationCanceledException e)
                {
                    var logger = context.RequestServices.GetRequiredService<ILogger<Startup>>();
                    logger.LogWarning(e, "Operation was canceled while waiting for semaphore");
                }
                finally
                {
                    semaphore.Release();
                }
            });
        }

        return app;
    }
}
