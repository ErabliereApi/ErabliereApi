using ErabliereApi.Authorization;
using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Donnees.Action.Put;
using ErabliereApi.Services;
using ErabliereApi.Services.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;

namespace ErabliereApi.Controllers;

/// <summary>
/// Contrôler permettant de gérer les clés d'API.
/// </summary>
[Route("access/[controller]")]
[ApiController]
public class ApiKeyController : ControllerBase
{
    private readonly ErabliereDbContext _context;
    private readonly IApiKeyService _apiApiKeyService;

    /// <summary>
    /// Constructeur
    /// </summary>
    /// <param name="context"></param>
    /// <param name="apiKeyService"></param>
    public ApiKeyController(ErabliereDbContext context, IApiKeyService apiKeyService)
    {
        _context = context;
        _apiApiKeyService = apiKeyService;
    }

    /// <summary>
    /// Permet de lister les clés d'API.
    /// </summary>
    [HttpGet]
    [EnableQuery]
    [Authorize(Roles = "administrateur", Policy = "TenantIdPrincipal")]
    public IQueryable<ApiKey> GetApiKeys()
    {
        return _context.ApiKeys.Select(k => new ApiKey
        {
            Id = k.Id,
            Name = k.Name,
            CreationTime = k.CreationTime,
            CustomerId = k.CustomerId,
            DeletionTime = k.DeletionTime,
            RevocationTime = k.RevocationTime,
            SubscriptionId = k.SubscriptionId,
            Key = "***",
            Customer = k.Customer
        });
    }

    /// <summary>
    /// Permet de créer une nouvelle clé d'API.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "administrateur", Policy = "TenantIdPrincipal")]
    public async Task<IActionResult> CreateApiKey([FromBody] PostApiKey postApiKey, CancellationToken token)
    {
        if (postApiKey.CustomerId == null)
        {
            ModelState.AddModelError("CustomerId", "CustomerId is required.");

            return BadRequest(new ValidationProblemDetails(ModelState));
        }

        var cust = await _context.Customers.FirstOrDefaultAsync(c => c.Id == postApiKey.CustomerId, token);

        if (cust == null)
        {
            ModelState.AddModelError("CustomerId", "Customer not found.");

            return BadRequest(new ValidationProblemDetails(ModelState));
        }

        var (apikey, originalKey) = await _apiApiKeyService.CreateApiKeyAsync(new CreateApiKeyParameters { Customer = cust, Name = postApiKey.Name }, token);

        return Ok(new
        {
            apikey.Id,
            apikey.Name,
            HeaderName = ApiKeyMiddleware.XApiKeyHeader,
            Key = originalKey,
            apikey.CreationTime,
            apikey.CustomerId
        });
    }

    /// <summary>
    /// Permet de modifier le nom d'une clé d'API.
    /// </summary>
    [HttpPut("{id}/name")]
    public async Task<IActionResult> UpdateApiKeyName(Guid id, [FromBody] PutApiKeyName param, CancellationToken token)
    {
        var user = UsersUtils.GetUniqueName(HttpContext.RequestServices.CreateScope(), User);

        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UniqueName == user, token);

        if (customer == null)
        {
            return Forbid();
        }

        var apiKey = await _context.ApiKeys.FirstOrDefaultAsync(k => k.Id == id, token);

        if (apiKey == null)
        {
            return NotFound();
        }

        if (apiKey.CustomerId != customer.Id)
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(param.Name))
        {
            ModelState.AddModelError("Name", "Name is required.");

            return BadRequest(new ValidationProblemDetails(ModelState));
        }

        apiKey.Name = param.Name;
        await _context.SaveChangesAsync(token);
        return NoContent();
    }

    /// <summary>
    /// Permet de révoquer une clé d'API.
    /// </summary>
    [HttpPut("{id}/revoke")]
    [Authorize(Roles = "administrateur", Policy = "TenantIdPrincipal")]
    public async Task<IActionResult> RevokeApiKey(Guid id, CancellationToken token)
    {
        var apiKey = await _context.ApiKeys.FirstOrDefaultAsync(k => k.Id == id, token);

        if (apiKey == null)
        {
            return NotFound();
        }

        apiKey.RevocationTime = DateTimeOffset.Now;

        await _context.SaveChangesAsync(token);

        return NoContent();
    }

    /// <summary>
    /// Permet de supprimer une clé d'API.
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(204)]
    public async Task<IActionResult> DeleteApiKey(Guid id, CancellationToken token)
    {
        if (!User.IsInRole("administrateur"))
        {
            using var scope = HttpContext.RequestServices.CreateScope();

            var unique_name = UsersUtils.GetUniqueName(scope, User);
#nullable disable
            var apiKey = await _context.ApiKeys.FirstOrDefaultAsync(k => k.Id == id && k.Customer.UniqueName == unique_name, token);
#nullable enable
            if (apiKey == null)
            {
                return NoContent();
            }

            _context.ApiKeys.Remove(apiKey);

            await _context.SaveChangesAsync(token);

            return NoContent();
        }
        else
        {
            var apiKey = await _context.ApiKeys.FirstOrDefaultAsync(k => k.Id == id, token);

            if (apiKey == null)
            {
                return NotFound();
            }

            _context.ApiKeys.Remove(apiKey);

            await _context.SaveChangesAsync(token);

            return NoContent();
        }
    }
}
