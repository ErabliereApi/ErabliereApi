using ErabliereApi.Depot.Sql;
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

    /// <summary>
    /// Constructeur
    /// </summary>
    /// <param name="context"></param>
    public ApiKeyController(ErabliereDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Permet de lister les clés d'API.
    /// </summary>
    [HttpGet]
    [EnableQuery]
    public IEnumerable<Donnees.ApiKey> GetApiKeys()
    {
        return _context.ApiKeys;
    }

    /// <summary>
    /// Permet de créer une nouvelle clé d'API.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateApiKey(CancellationToken token)
    {
        var newApiKey = new Donnees.ApiKey
        {
            Id = Guid.NewGuid(),
            Key = Guid.NewGuid().ToString(),
            CreationTime = DateTime.Now
        };

        await _context.ApiKeys.AddAsync(newApiKey, token);

        await _context.SaveChangesAsync(token);

        return Ok(newApiKey);
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
