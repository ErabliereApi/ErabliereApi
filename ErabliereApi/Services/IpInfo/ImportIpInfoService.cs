using ErabliereApi.Depot.Sql;
using ErabliereApi.Extensions;
using ErabliereApi.Models;

namespace ErabliereApi.Services.IpInfo;

/// <summary>
/// Service pour importer les informations IP ASN à partir d'un fichier CSV ou Excel
/// </summary>
public class ImportIpInfoService
{
    private readonly ErabliereDbContext _context;
    private readonly ILogger<ImportIpInfoService> _logger;

    /// <summary>
    /// Constructeur
    /// </summary>
    public ImportIpInfoService(ErabliereDbContext context, ILogger<ImportIpInfoService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Importe les informations IP ASN à partir d'un flux de fichier
    /// </summary>
    /// <param name="file">Flux du fichier à importer</param>
    /// <param name="importIfNotEmpty">Indique si l'importation doit être effectuée peut importe le contenue de la table</param>
    /// <param name="cancellationToken">Jeton d'annulation</param>
    /// <returns>Nombre total d'enregistrements sauvegardés</returns>
    public async Task<int> ImportIpInfoAsync(Stream file, bool importIfNotEmpty, CancellationToken cancellationToken)
    {
        if (!importIfNotEmpty && _context.IpNetworkAsnInfos.Any())
        {
            _logger.LogInformation("La table IpNetworkAsnInfos n'est pas vide. L'importation est ignorée.");
            return 0;
        }

        _context.ChangeTracker.AutoDetectChangesEnabled = false;

        var bufferSize = 20000;
        var buffer = new List<IpNetworkAsnInfo>(bufferSize);
        var totalSaved = 0;
        var loopHasBegun = false;
        var rowNumber = 2;

        await Console.Out.WriteLineAsync($"{DateTimeOffset.UtcNow} Début de l'importation des informations IP ASN...");
        using var reader = new StreamReader(file);
        await Console.Out.WriteLineAsync($"{DateTimeOffset.UtcNow} Fichier chargé en mémoire.");
        await Console.Out.WriteLineAsync($"{DateTimeOffset.UtcNow} Début du parcourt des lignes network IP ASN...");

        // skip the first line (header)
        await reader.ReadLineAsync();

        do
        {
            var line = await reader.ReadLineAsync();

            if (string.IsNullOrWhiteSpace(line))
            {
                break;
            }

            var cells = line.SplitCSVLine();

            if (!loopHasBegun)
            {
                loopHasBegun = true;
                await Console.Out.WriteLineAsync($"{DateTimeOffset.UtcNow} Parcourt des lignes commencé...");
            }

            if (cells.Count <= 2)
            {
                _logger.LogWarning("Ligne ignorée en raison d'un nombre insuffisant de colonnes : {RowNumber}", rowNumber);
                continue;
            }

            if (cells.Count >= 9)
            {
                _logger.LogWarning("Ligne ignorée en raison d'un nombre suppérieur de colonnes : {RowNumber}", rowNumber);
                continue;
            }

            var ipNetworkAsnInfo = new IpNetworkAsnInfo
            {
                Network = cells.AtIndexOrDefault(0) ?? string.Empty,
                Country = cells.AtIndexOrDefault(1) ?? string.Empty,
                CountryCode = cells.AtIndexOrDefault(2) ?? string.Empty,
                Continent = cells.AtIndexOrDefault(3) ?? string.Empty,
                ContinentCode = cells.AtIndexOrDefault(4) ?? string.Empty,
                ASN = cells.AtIndexOrDefault(5) ?? string.Empty,
                AS_Name = (cells.AtIndexOrDefault(0) ?? string.Empty).Trim(),
                AS_Domain = cells.AtIndexOrDefault(7) ?? string.Empty,
            };

            if (ipNetworkAsnInfo.AS_Name.Length > 200)
            {
                _logger.LogWarning("Troncature du nom AS pour le réseau {Network} car il dépasse 200 caractères. Text: {AS_Name}", ipNetworkAsnInfo.Network, ipNetworkAsnInfo.AS_Name);

                ipNetworkAsnInfo.AS_Name = ipNetworkAsnInfo.AS_Name[..200];
            }

            buffer.Add(ipNetworkAsnInfo);

            if (buffer.Count >= bufferSize)
            {
                var t = Console.Out.WriteLineAsync($"{DateTimeOffset.UtcNow} Sauvegarde de {buffer.Count} enregistrements... Total: {totalSaved}");
                await _context.IpNetworkAsnInfos.AddRangeAsync(buffer, cancellationToken);
                totalSaved += await _context.SaveChangesAsync(cancellationToken);
                buffer.Clear();
                await t;
            }

            rowNumber++;

        } while (!reader.EndOfStream);

        totalSaved += await _context.SaveChangesAsync(cancellationToken);

        await Console.Out.WriteLineAsync($"Total des enregistrements sauvegardés : {totalSaved}");

        _context.ChangeTracker.AutoDetectChangesEnabled = true;

        return totalSaved;
    }
}