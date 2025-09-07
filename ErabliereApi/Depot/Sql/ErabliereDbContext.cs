using ErabliereApi.Donnees;
using ErabliereApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace ErabliereApi.Depot.Sql
{
    /// <summary>
    /// Classe DbContext pour interagir avec la base de donnée en utilisant EntityFramework
    /// </summary>
    public class ErabliereDbContext : DbContext
    {
        /// <summary>
        /// Constructeur par initialisation
        /// </summary>
        /// <param name="options"></param>
#pragma warning disable CS8618 // Un champ non-nullable doit contenir une valeur non-null lors de la fermeture du constructeur. Le constructeur de base s'occupe d'initialiser les propriétés.
        public ErabliereDbContext([NotNull] DbContextOptions options) : base(options)
#pragma warning restore CS8618 // Un champ non-nullable doit contenir une valeur non-null lors de la fermeture du constructeur. Le constructeur de base s'occupe d'initialiser les propriétés.
        {

        }

#pragma warning disable S1144 // Unused private types or members should be removed
        /// <summary>
        /// Table des alertes
        /// </summary>
        public DbSet<Alerte> Alertes { get; private set; }

        /// <summary>
        /// Table des alertes des capteurs
        /// </summary>
        public DbSet<AlerteCapteur> AlerteCapteurs { get; private set; }

        /// <summary>
        /// Table des appareils
        /// </summary>
        public DbSet<Appareil> Appareils { get; private set; }

        /// <summary>
        /// Table des barils
        /// </summary>
        public DbSet<Baril> Barils { get; private set; }

        /// <summary>
        /// Table des dompeux
        /// </summary>
        public DbSet<Dompeux> Dompeux { get; private set; }

        /// <summary>
        /// Table des données du trio de données
        /// </summary>
        public DbSet<Donnee> Donnees { get; private set; }

        /// <summary>
        /// Table des érablières
        /// </summary>
        public DbSet<Erabliere> Erabliere { get; private set; }

        /// <summary>
        /// Table des capteurs
        /// </summary>
        public DbSet<Capteur> Capteurs { get; private set; }

        /// <summary>
        /// Table des styles de capteurs
        /// </summary>
        public DbSet<CapteurStyle> CapteurStyles { get; private set; }

        /// <summary>
        /// Table des capteurs d'images
        /// </summary>
        public DbSet<CapteurImage> CapteurImage { get; private set; }

        /// <summary>
        /// Table des données des capteurs
        /// </summary>
        public DbSet<DonneeCapteur> DonneesCapteur { get; private set; }

        /// <summary>
        /// Table des notes
        /// </summary>
        public DbSet<Note> Notes { get; private set; }

        /// <summary>
        /// Table des rappels
        /// </summary>
        public DbSet<Rappel> Rappels { get; private set; }

        /// <summary>
        /// Table de la docuemntation
        /// </summary>
        public DbSet<Documentation> Documentation { get; private set; }

        /// <summary>
        /// Table des utilisateurs
        /// </summary>
        public DbSet<Customer> Customers { get; private set; }

        /// <summary>
        /// Table des clé d'api
        /// </summary>
        public DbSet<ApiKey> ApiKeys { get; private set; }

        /// <summary>
        /// Table de jonction entre les érablières et les utilisateurs
        /// </summary>
        public DbSet<CustomerErabliere> CustomerErablieres { get; private set; }

        /// <summary>
        /// Table des conversations
        /// </summary>
        public DbSet<Conversation> Conversations { get; private set; }

        /// <summary>
        /// Table des messages
        /// </summary>
        public DbSet<Message> Messages { get; private set; }

        /// <summary>
        /// Table des rapports
        /// </summary>
        public DbSet<Rapport> Rapports { get; private set; }

        /// <summary>
        /// Table des données de rapports
        /// </summary>
        public DbSet<RapportDonnees> DonneesRapports { get; private set; }

        /// <summary>
        /// Table des horaires
        /// </summary>
        public DbSet<Horaire> Horaires { get; private set; }

        /// <summary>
        /// Table des IPs des utilisateurs
        /// </summary>
        public DbSet<IpInfo> IpInfos { get; private set; }

        /// <summary>
        /// Table des IPs et ASN importées de la base de données tierce
        /// </summary>
        public DbSet<IpNetworkAsnInfo> IpNetworkAsnInfos { get; private set; }
#pragma warning restore S1144 // Unused private types or members should be removed

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ErabliereDbContext).Assembly);
        }

        /// <inheritdoc />
        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);

            configurationBuilder.Properties<decimal?>()
                .HavePrecision(18, 6);
        }

        /// <summary>
        /// Try to save changes to the database.
        /// If succeeded, it return the number of state entry modified and no exception.
        /// If failed, it return 0 as the number of state entry modified and the exception that occure.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="logger">Optionnal logger</param>
        /// <returns></returns>
        public async Task<(int, Exception?)> TrySaveChangesAsync(CancellationToken token, ILogger? logger = null)
        {
            try
            {
                return (await SaveChangesAsync(token), null);
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Error while tying to save changes to the database");
                return (0, e);
            }
        }
    }
}
