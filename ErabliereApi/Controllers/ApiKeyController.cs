using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;

namespace ErabliereApi.Controllers;

/// <summary>
/// Contrôler permettant de gérer les clés d'API.
/// </summary>
[Route("access/[controller]")]
[ApiController]
[Authorize(Roles = "administrateur", Policy = "TenantIdPrincipal")]
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
    public IQueryable<Donnees.ApiKey> GetApiKeys()
    {
        return _context.ApiKeys.Select(k => new Donnees.ApiKey
        {
            Id = k.Id,
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

        var (apikey, originalKey) = await _apiApiKeyService.CreateApiKeyAsync(cust, token);

        return Ok(new ApiKey {
            Id = apikey.Id,
            Key = originalKey,
            CreationTime = apikey.CreationTime,
            CustomerId = apikey.CustomerId
        });
    }

    /// <summary>
    /// Permet de révoquer une clé d'API.
    /// </summary>
    [HttpPut("{id}/revoke")]
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
    public async Task<IActionResult> DeleteApiKey(Guid id, CancellationToken token)
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
