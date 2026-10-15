using ErabliereApi.Attributes;
using ErabliereApi.Authorization;
using ErabliereApi.Controllers.Base;
using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Donnees.Action.Get;
using ErabliereApi.Donnees.Action.Post;
using ErabliereApi.Extensions;
using ErabliereApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ErabliereApi.Controllers;

/// <summary>
/// Contrôleur pour la mise en service des kits matériels d'une érablière
/// </summary>
[ApiController]
[Route("Erablieres/{id}/[controller]")]
[Authorize]
public class KitsController : ErabliereApiBaseController
{
    private static readonly JsonSerializerOptions _jsonIndente = new() { WriteIndented = true };

    private readonly ErabliereDbContext _depot;
    private readonly IApiKeyService _apiKeyService;
    private readonly IConfiguration _config;

    /// <summary>
    /// Constructeur par initialisation
    /// </summary>
    public KitsController(
        IServiceProvider serviceProvider,
        ErabliereDbContext depot,
        IConfiguration config,
        IApiKeyService apiKeyService) : base(serviceProvider, depot, config)
    {
        _depot = depot;
        _apiKeyService = apiKeyService;
        _config = config;
    }

    /// <summary>
    /// Lister les kits d'une érablière
    /// </summary>
    /// <param name="id">L'id de l'érablière</param>
    /// <param name="token">Jeton d'annulation de la tâche</param>
    /// <response code="200">Les kits de l'érablière, actifs et révoqués</response>
    [HttpGet]
    [ValiderOwnership("id")]
    [ProducesResponseType(200, Type = typeof(GetKit[]))]
    public async Task<IActionResult> Lister(Guid id, CancellationToken token)
    {
        // ValiderOwnership laisse passer la lecture d'une érablière publique,
        // la liste des kits reste réservée aux utilisateurs qui y ont accès.
        if (_config.IsAuthEnabled() && !await ADroitAccesAsync(id, token))
        {
            return Forbid();
        }

        var kits = await _depot.Kits
            .AsNoTracking()
            .Where(k => k.IdErabliere == id)
            .OrderBy(k => k.DC)
            .Select(k => new GetKit
            {
                Id = k.Id,
                Nom = k.Nom,
                Type = k.Type,
                IdCapteurAlimentation = k.IdCapteurAlimentation,
                IdCapteurCharge = k.IdCapteurCharge,
                IdCapteurAutonomie = k.IdCapteurAutonomie,
                DC = k.DC,
                DateRevocation = k.DateRevocation,
                DerniereUtilisation = _depot.ApiKeys
                    .Where(a => a.Id == k.IdApiKey)
                    .Select(a => a.LastUsage)
                    .FirstOrDefault()
            })
            .ToArrayAsync(token);

        return Ok(kits);
    }

    /// <summary>
    /// Ajouter un kit de détection de panne de courant. Crée les capteurs d'alimentation, de
    /// charge et d'autonomie, l'alerte de panne, l'alerte de silence et une clé d'API restreinte
    /// à l'envoi de données vers ces trois capteurs.
    /// </summary>
    /// <remarks>
    /// La clé d'API n'est pas envoyée par courriel et n'est retournée qu'une seule fois, dans
    /// les fichiers de configuration à copier dans ~/.erabliereapi/ sur le kit.
    /// </remarks>
    /// <param name="id">L'id de l'érablière</param>
    /// <param name="kit">Les paramètres du kit</param>
    /// <param name="token">Jeton d'annulation de la tâche</param>
    /// <response code="200">Le kit est créé, avec sa configuration</response>
    /// <response code="400">Aucun utilisateur n'est associé à la requête</response>
    [HttpPost("Panne")]
    [ValiderOwnership("id")]
    [ProducesResponseType(200, Type = typeof(PostKitPanneResponse))]
    public async Task<IActionResult> AjouterKitPanne(Guid id, PostKitPanne kit, CancellationToken token)
    {
        var (_, _, customer) = await IsAuthenticatedAsync(token);

        if (customer?.Id == null)
        {
            return BadRequest("La clé d'API d'un kit agit au nom d'un utilisateur, l'authentification est requise.");
        }

        var url = string.IsNullOrWhiteSpace(kit.Url) ? $"{Request.Scheme}://{Request.Host}{Request.PathBase}" : kit.Url.Trim();

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uriApi) ||
            (uriApi.Scheme != Uri.UriSchemeHttps && uriApi.Scheme != Uri.UriSchemeHttp))
        {
            ModelState.AddModelError(nameof(kit.Url), "L'adresse de l'API doit être une adresse http ou https absolue.");

            return BadRequest(new ValidationProblemDetails(ModelState));
        }

        var maintenant = DateTimeOffset.Now;
        var nom = string.IsNullOrWhiteSpace(kit.Nom) ? "Kit panne" : kit.Nom.Trim();
        var indiceOrdre = await _depot.Capteurs.CountAsync(c => c.IdErabliere == id, token);

        Capteur NouveauCapteur(string suffixe, string? symbole) => new()
        {
            Id = Guid.NewGuid(),
            IdErabliere = id,
            Nom = $"{nom} {suffixe}",
            Symbole = symbole,
            AfficherCapteurDashboard = true,
            IndiceOrdre = indiceOrdre++,
            DC = maintenant
        };

        var alimentation = NouveauCapteur("alimentation", null);
        var charge = NouveauCapteur("charge", "%");
        var autonomie = NouveauCapteur("autonomie", "min");

        await _depot.Capteurs.AddRangeAsync([alimentation, charge, autonomie], token);

        var envoyerA = string.IsNullOrWhiteSpace(kit.EnvoyerA) ? customer.Email : kit.EnvoyerA;

        await _depot.AlerteCapteurs.AddRangeAsync(
        [
            // Le kit envoie 0 sur le capteur d'alimentation au passage sur batterie
            new AlerteCapteur
            {
                Id = Guid.NewGuid(),
                IdCapteur = alimentation.Id,
                Nom = $"{nom} : panne de courant",
                EnvoyerA = envoyerA,
                TexterA = kit.TexterA,
                MinValue = 0,
                IsEnable = true,
                DC = maintenant.LocalDateTime
            },
            // La charge est envoyée à chaque minute. Quand le routeur tombe avec le courant,
            // la donnée de panne attend le retour du réseau et seul le silence avertit.
            new AlerteCapteur
            {
                Id = Guid.NewGuid(),
                IdCapteur = charge.Id,
                Nom = $"{nom} : kit muet",
                EnvoyerA = envoyerA,
                TexterA = kit.TexterA,
                DelaiSilenceMinutes = kit.DelaiSilenceMinutes,
                IsEnable = true,
                DC = maintenant.LocalDateTime
            }
        ], token);

        // Sans SubscriptionId : un kit envoie au moins une requête par minute,
        // elles ne doivent pas être comptées comme de l'utilisation facturable.
        var (apiKey, cleOriginale) = _apiKeyService.PreparerApiKey(customer.Id.Value, Tronquer($"Kit {nom}", 50));
        apiKey.Id = Guid.NewGuid();
        apiKey.AuthorizeVerbs = "POST";
        apiKey.AuthorizeUris = string.Join("|||",
            new[] { alimentation, charge, autonomie }.Select(c => $"/Capteurs/{c.Id}/DonneesCapteurV2"));

        await _depot.ApiKeys.AddAsync(apiKey, token);

        var entite = new Kit
        {
            Id = Guid.NewGuid(),
            IdErabliere = id,
            Nom = nom,
            Type = Kit.TypePanne,
            IdApiKey = apiKey.Id,
            IdCapteurAlimentation = alimentation.Id,
            IdCapteurCharge = charge.Id,
            IdCapteurAutonomie = autonomie.Id,
            DC = maintenant
        };

        await _depot.Kits.AddAsync(entite, token);

        await _depot.SaveChangesAsync(token);

        // Le proxy python nomme le fichier de la clé d'après l'hôte de l'url (port compris).
        // L'url est normalisée pour que les deux concordent.
        var urlKit = (uriApi.GetLeftPart(UriPartial.Authority) + uriApi.AbsolutePath).TrimEnd('/');

        return Ok(new PostKitPanneResponse
        {
            IdKit = entite.Id.Value,
            NomFichierConfiguration = "monitorUps.json",
            FichierConfiguration = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["ups"] = "ups@localhost",
                ["url"] = urlKit,
                ["authType"] = "ApiKey",
                ["capteurCharge"] = charge.Id.ToString()!,
                ["capteurAlimentation"] = alimentation.Id.ToString()!,
                ["capteurAutonomie"] = autonomie.Id.ToString()!
            }, _jsonIndente),
            NomFichierAuthentification = $"auth.{uriApi.Authority}.config",
            FichierAuthentification = JsonSerializer.Serialize(
                new[] { new Dictionary<string, string> { ["ApiKey"] = cleOriginale } }, _jsonIndente)
        });
    }

    /// <summary>
    /// Révoquer un kit. Sa clé d'API cesse de fonctionner. Les capteurs, les alertes et
    /// l'historique des données sont conservés.
    /// </summary>
    /// <param name="id">L'id de l'érablière</param>
    /// <param name="idKit">L'id du kit</param>
    /// <param name="token">Jeton d'annulation de la tâche</param>
    /// <response code="204">Le kit est révoqué</response>
    /// <response code="404">Le kit n'existe pas dans cette érablière</response>
    [HttpDelete("{idKit}")]
    [ValiderOwnership("id")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Revoquer(Guid id, Guid idKit, CancellationToken token)
    {
        var kit = await _depot.Kits.FirstOrDefaultAsync(k => k.Id == idKit && k.IdErabliere == id, token);

        if (kit == null)
        {
            return NotFound();
        }

        if (kit.DateRevocation == null)
        {
            var maintenant = DateTimeOffset.Now;

            kit.DateRevocation = maintenant;
            kit.DM = maintenant;

            await RevoquerClesAsync(_depot, [kit], maintenant, token);

            await _depot.SaveChangesAsync(token);
        }

        return NoContent();
    }

    /// <summary>
    /// Révoquer les clés d'API des kits donnés. Ne sauvegarde pas les changements.
    /// </summary>
    internal static async Task RevoquerClesAsync(ErabliereDbContext depot, IEnumerable<Kit> kits, DateTimeOffset maintenant, CancellationToken token)
    {
        var idCles = kits.Where(k => k.IdApiKey != null).Select(k => k.IdApiKey).ToArray();

        var cles = await depot.ApiKeys
            .Where(a => idCles.Contains(a.Id) && a.RevocationTime == null)
            .ToArrayAsync(token);

        foreach (var cle in cles)
        {
            cle.RevocationTime = maintenant;
        }
    }

    private async Task<bool> ADroitAccesAsync(Guid id, CancellationToken token)
    {
        var (authentifie, _, customer) = await IsAuthenticatedAsync(token);

        return authentifie && customer != null &&
            await _depot.CustomerErablieres.AnyAsync(c => c.IdErabliere == id && c.IdCustomer == customer.Id, token);
    }

    private static string Tronquer(string valeur, int longueur)
    {
        return valeur.Length <= longueur ? valeur : valeur[..longueur];
    }
}
