using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Donnees.Action.Post;
using ErabliereApi.Donnees.Action.Put;
using ErabliereApi.Extensions;
using ErabliereApi.Services;
using ErabliereApi.Services.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErabliereApi.Controllers;

/// <summary>
/// Contrôler représentant les abonnements de l'utilisateur authentifié.
/// Les abonnements payants sont créés via une session de paiement Stripe et
/// synchronisés par le webhook <see cref="CheckoutController.Webhook" />.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AbonnementsController : ControllerBase
{
    private readonly ErabliereDbContext _depot;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Constructeur par initialisation
    /// </summary>
    /// <param name="depot">La base de données</param>
    /// <param name="configuration">La configuration de l'application</param>
    public AbonnementsController(ErabliereDbContext depot, IConfiguration configuration)
    {
        _depot = depot;
        _configuration = configuration;
    }

    /// <summary>
    /// Lister les abonnements de l'utilisateur authentifié
    /// </summary>
    /// <response code="200">Une liste d'abonnements potentiellement vide.</response>
    /// <response code="404">L'utilisateur authentifié n'existe pas.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Abonnement>), 200)]
    public async Task<IActionResult> Lister(CancellationToken token)
    {
        var customer = await GetCustomerAuthentifieAsync(token);

        if (customer == null)
        {
            return NotFound("Utilisateur non trouvé.");
        }

        var abonnements = await _depot.Abonnements.AsNoTracking()
            .Where(a => a.CustomerId == customer.Id)
            .OrderByDescending(a => a.DC)
            .ToArrayAsync(token);

        return Ok(abonnements);
    }

    /// <summary>
    /// Consulter un abonnement de l'utilisateur authentifié
    /// </summary>
    /// <param name="idAbonnement">L'identifiant de l'abonnement</param>
    /// <param name="token">Le token d'annulation</param>
    /// <response code="200">L'abonnement demandé.</response>
    /// <response code="404">L'abonnement n'existe pas ou n'appartient pas à l'utilisateur.</response>
    [HttpGet("{idAbonnement}")]
    [ProducesResponseType(typeof(Abonnement), 200)]
    public async Task<IActionResult> Consulter(Guid idAbonnement, CancellationToken token)
    {
        var (abonnement, erreur) = await TrouverAbonnementDuCustomerAsync(idAbonnement, token);

        if (erreur != null)
        {
            return erreur;
        }

        return Ok(abonnement);
    }

    /// <summary>
    /// Créer un abonnement pour l'utilisateur authentifié.
    /// Pour un forfait payant, une session de paiement Stripe est créée et l'abonnement
    /// reste en attente jusqu'à la confirmation du paiement par le webhook.
    /// </summary>
    /// <param name="abonnement">L'abonnement à créer</param>
    /// <param name="token">Le token d'annulation</param>
    /// <response code="200">L'id de l'abonnement créé et, pour un forfait payant, l'url de paiement.</response>
    /// <response code="400">La validation de l'abonnement a échoué.</response>
    [HttpPost]
    public async Task<IActionResult> Ajouter(PostAbonnement abonnement, CancellationToken token)
    {
        if (!ForfaitsAbonnement.EstValide(abonnement.Plan))
        {
            return BadRequest($"Le forfait '{abonnement.Plan}' n'est pas un forfait valide. " +
                              $"Les forfaits valides sont : {string.Join(", ", ForfaitsAbonnement.Tous)}.");
        }

        if (!Abonnement.DatesValides(abonnement.DateDebut, abonnement.DateFin))
        {
            return BadRequest("La date de début doit précéder la date de fin.");
        }

        var estPayant = ForfaitsAbonnement.EstPayant(abonnement.Plan);

        if (estPayant && !FrequencesFacturation.EstValide(abonnement.FrequenceFacturation))
        {
            return BadRequest($"La fréquence de facturation '{abonnement.FrequenceFacturation}' n'est pas valide " +
                              $"pour un forfait payant. Les fréquences valides sont : {string.Join(", ", FrequencesFacturation.Toutes)}.");
        }

        var customer = await GetCustomerAuthentifieAsync(token);

        if (customer == null)
        {
            return NotFound("Utilisateur non trouvé.");
        }

        var abonnementEnCours = await _depot.Abonnements.AsNoTracking()
            .AnyAsync(a => a.CustomerId == customer.Id &&
                           (a.Statut == StatutAbonnement.Actif || a.Statut == StatutAbonnement.EnAttente), token);

        if (abonnementEnCours)
        {
            return BadRequest("Un abonnement actif ou en attente existe déjà. " +
                              "Annulez l'abonnement en cours avant d'en créer un nouveau.");
        }

        if (estPayant && !_configuration.StripeIsEnabled())
        {
            return BadRequest("Le paiement n'est pas activé sur ce serveur. Impossible de créer un abonnement payant.");
        }

        string? checkoutUrl = null;

        if (estPayant)
        {
            var checkoutService = HttpContext.RequestServices.GetRequiredService<ICheckoutService>();

            try
            {
                var session = await checkoutService.CreateAbonnementSessionAsync(abonnement.FrequenceFacturation!, token);

                checkoutUrl = session.Url;
            }
            catch (InvalidOperationException e)
            {
                return BadRequest(e.Message);
            }
        }

        var entity = await _depot.Abonnements.AddAsync(new Abonnement
        {
            CustomerId = customer.Id!.Value,
            Plan = abonnement.Plan!.ToLowerInvariant(),
            FrequenceFacturation = estPayant ? abonnement.FrequenceFacturation!.ToLowerInvariant() : null,
            DateDebut = abonnement.DateDebut ?? (estPayant ? null : DateTimeOffset.Now),
            DateFin = abonnement.DateFin,
            Statut = estPayant ? StatutAbonnement.EnAttente : StatutAbonnement.Actif,
            DC = DateTimeOffset.Now,
            DM = DateTimeOffset.Now
        }, token);

        await _depot.SaveChangesAsync(token);

        if (checkoutUrl != null)
        {
            return Ok(new { id = entity.Entity.Id, checkoutUrl });
        }

        return Ok(new { id = entity.Entity.Id });
    }

    /// <summary>
    /// Modifier un abonnement de l'utilisateur authentifié.
    /// Seuls le forfait et les dates peuvent être modifiés. Le statut change via
    /// l'annulation (<see cref="Annuler" />) ou les webhooks Stripe.
    /// </summary>
    /// <param name="idAbonnement">L'identifiant de l'abonnement</param>
    /// <param name="abonnement">Les modifications à apporter</param>
    /// <param name="token">Le token d'annulation</param>
    /// <response code="204">L'abonnement a été correctement modifié.</response>
    /// <response code="400">La validation de l'abonnement a échoué.</response>
    /// <response code="404">L'abonnement n'existe pas ou n'appartient pas à l'utilisateur.</response>
    [HttpPut("{idAbonnement}")]
    public async Task<IActionResult> Modifier(Guid idAbonnement, PutAbonnement abonnement, CancellationToken token)
    {
        if (abonnement.Id != null && abonnement.Id != idAbonnement)
        {
            return BadRequest("L'id de la route ne concorde pas avec l'id de l'abonnement à modifier.");
        }

        if (abonnement.Plan != null && !ForfaitsAbonnement.EstValide(abonnement.Plan))
        {
            return BadRequest($"Le forfait '{abonnement.Plan}' n'est pas un forfait valide. " +
                              $"Les forfaits valides sont : {string.Join(", ", ForfaitsAbonnement.Tous)}.");
        }

        var (entity, erreur) = await TrouverAbonnementDuCustomerAsync(idAbonnement, token, asNoTracking: false);

        if (erreur != null)
        {
            return erreur;
        }

        if (entity!.Statut is StatutAbonnement.Annule or StatutAbonnement.Expire)
        {
            return BadRequest("Un abonnement annulé ou expiré ne peut pas être modifié.");
        }

        if (abonnement.Plan != null &&
            ForfaitsAbonnement.EstPayant(abonnement.Plan) &&
            !ForfaitsAbonnement.EstPayant(entity.Plan) &&
            entity.StripeSubscriptionId == null)
        {
            return BadRequest("Le passage à un forfait payant doit se faire en annulant l'abonnement " +
                              "en cours puis en créant un nouvel abonnement, afin de compléter le paiement.");
        }

        var dateDebut = abonnement.DateDebut ?? entity.DateDebut;
        var dateFin = abonnement.DateFin ?? entity.DateFin;

        if (!Abonnement.DatesValides(dateDebut, dateFin))
        {
            return BadRequest("La date de début doit précéder la date de fin.");
        }

        if (abonnement.Plan != null)
        {
            entity.Plan = abonnement.Plan.ToLowerInvariant();
        }

        entity.DateDebut = dateDebut;
        entity.DateFin = dateFin;
        entity.DM = DateTimeOffset.Now;

        await _depot.SaveChangesAsync(token);

        return NoContent();
    }

    /// <summary>
    /// Annuler un abonnement de l'utilisateur authentifié.
    /// L'abonnement n'est pas supprimé : son statut passe à Annulé. Si l'abonnement
    /// est relié à un abonnement Stripe, celui-ci est également annulé.
    /// </summary>
    /// <param name="idAbonnement">L'identifiant de l'abonnement à annuler</param>
    /// <param name="token">Le token d'annulation</param>
    /// <response code="204">L'abonnement a été correctement annulé.</response>
    /// <response code="400">L'abonnement ne peut pas être annulé dans son statut actuel.</response>
    /// <response code="404">L'abonnement n'existe pas ou n'appartient pas à l'utilisateur.</response>
    [HttpDelete("{idAbonnement}")]
    public async Task<IActionResult> Annuler(Guid idAbonnement, CancellationToken token)
    {
        var (entity, erreur) = await TrouverAbonnementDuCustomerAsync(idAbonnement, token, asNoTracking: false);

        if (erreur != null)
        {
            return erreur;
        }

        if (!entity!.PeutTransitionnerVers(StatutAbonnement.Annule))
        {
            return BadRequest($"Un abonnement au statut {entity.Statut} ne peut pas être annulé.");
        }

        if (entity.StripeSubscriptionId != null && _configuration.StripeIsEnabled())
        {
            var checkoutService = HttpContext.RequestServices.GetRequiredService<ICheckoutService>();

            await checkoutService.CancelSubscriptionAsync(entity.StripeSubscriptionId, token);
        }

        entity.ChangerStatut(StatutAbonnement.Annule);
        entity.DateFin ??= DateTimeOffset.Now;

        await _depot.SaveChangesAsync(token);

        return NoContent();
    }

    /// <summary>
    /// Retrouve l'abonnement demandé s'il appartient à l'utilisateur authentifié,
    /// sinon retourne le résultat d'erreur à renvoyer au client.
    /// </summary>
    private async Task<(Abonnement?, IActionResult?)> TrouverAbonnementDuCustomerAsync(
        Guid idAbonnement, CancellationToken token, bool asNoTracking = true)
    {
        var customer = await GetCustomerAuthentifieAsync(token);

        if (customer == null)
        {
            return (null, NotFound("Utilisateur non trouvé."));
        }

        var query = asNoTracking ? _depot.Abonnements.AsNoTracking() : _depot.Abonnements;

        var abonnement = await query.FirstOrDefaultAsync(a => a.Id == idAbonnement, token);

        if (abonnement == null || abonnement.CustomerId != customer.Id)
        {
            return (null, NotFound());
        }

        return (abonnement, null);
    }

    /// <summary>
    /// Retrouve le customer de l'utilisateur authentifié à partir de ses claims.
    /// </summary>
    private async Task<Customer?> GetCustomerAuthentifieAsync(CancellationToken token)
    {
        using var scope = HttpContext.RequestServices.CreateScope();

        var uniqueName = UsersUtils.GetUniqueName(scope, User);

        if (string.IsNullOrWhiteSpace(uniqueName))
        {
            return null;
        }

        return await _depot.Customers.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UniqueName == uniqueName, token);
    }
}
