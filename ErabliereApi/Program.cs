using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;

namespace ErabliereApi
{
    /// <summary>
    /// Classe Program, contient le point d'entr� du programme.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Fonction main. le point d'entr� du programme.
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            try
            {
                Log.Information("Starting up");
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application start-up failed");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        /// <summary>
        /// Cr�ation de l'application
        /// </summary>
        /// <param name="args">Les arguments re�u de la ligne de commande</param>
        /// <returns>Une nouvelle instance de <see cref="IHostBuilder"/></returns>
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging((hostBuildContext, builder) =>
                {
                    builder.AddConsole();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
