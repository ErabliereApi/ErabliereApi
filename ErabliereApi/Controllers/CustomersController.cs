﻿using System.Security.Claims;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using ErabliereApi.Depot.Sql;
using ErabliereApi.Donnees;
using ErabliereApi.Donnees.Action.Get;
using ErabliereApi.Donnees.Action.Put;
using ErabliereApi.Services.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace ErabliereApi.Controllers;

/// <summary>
/// Contrôler permettant d'accéder au information de base sur les utilisateurs.
/// </summary>
[ApiController]
[Route("Customers")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ErabliereDbContext _context;
    private readonly IMapper _mapper;
    private readonly IDistributedCache _cache;

    /// <summary>
    /// Constructeur avec dépendance
    /// </summary>
    /// <param name="context">La base de données</param>
    /// <param name="mapper">Le mapper</param>
    /// <param name="cache">Le cache distribué</param>
    public CustomersController(ErabliereDbContext context, IMapper mapper, IDistributedCache cache)
    {
        _context = context;
        _mapper = mapper;
        _cache = cache;
    }

    /// <summary>
    /// Point de terminaison pour récupérer le profil de l'utilisateur authentifié.
    ///</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(Customer), 200)]
    public async Task<IActionResult> GetProfile(CancellationToken token)
    {
        using var scope = HttpContext.RequestServices.CreateScope();

        var unique_name = UsersUtils.GetUniqueName(scope, User);

        var customer = await _context.Customers
            .Include(c => c.ApiKeys)
            .FirstOrDefaultAsync(c => c.UniqueName == unique_name, token);

        if (customer == null)
        {
            return NotFound("Utilisateur non trouvé: " + unique_name);
        }

        customer.ApiKeys = customer.ApiKeys?.Select(k => new ApiKey
        {
            Id = k.Id,
            Name = k.Name,
            CreationTime = k.CreationTime,
            CustomerId = k.CustomerId,
            DeletionTime = k.DeletionTime,
            RevocationTime = k.RevocationTime,
            SubscriptionId = k.SubscriptionId,
            Key = "***", // Masquer la clé pour des raisons de sécurité
            Customer = null // Ne pas inclure le client dans la clé API
        }).ToList();

        return Ok(customer);
    }

    /// <summary>
    /// Point de terminaison pour vérifier si l'utilisateur a accepté les conditions d'utilisation.
    /// Si l'utilisateur n'existe pas, un code 404 est retourné.
    /// Si l'utilisateur a accepté les conditions, un code 200 est retourné avec la date d'acceptation.
    /// Si l'utilisateur n'a pas accepté les conditions, un code 200 est retourné avec un champ `hasAcceptedTerms` à false.
    /// </summary>
    [HttpGet("me/has-accepted-terms")]
    [Authorize]
    [ProducesResponseType(200, Type = typeof(object))]
    public async Task<IActionResult> HasAcceptedTerms(CancellationToken token)
    {
        using var scope = HttpContext.RequestServices.CreateScope();

        var unique_name = UsersUtils.GetUniqueName(scope, User);

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.UniqueName == unique_name, token);

        if (customer == null)
        {
            return NotFound("Utilisateur non trouvé: " + unique_name);
        }

        return Ok(new
        {
            hasAcceptedTerms = customer.AcceptTermsAt != null,
            acceptTermsAt = customer.AcceptTermsAt
        });
    }

    /// <summary>
    /// Point de terminaison pour qu'un utilisateur accepte les conditions d'utilisation.
    /// Si l'utilisateur a déjà accepté les conditions, un code 400 est retourné.
    /// </summary>
    [HttpPost("me/accept-terms")]
    [Authorize]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> AcceptTerms(CancellationToken token)
    {
        using var scope = HttpContext.RequestServices.CreateScope();

        var unique_name = UsersUtils.GetUniqueName(scope, User);

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.UniqueName == unique_name, token);

        if (customer == null)
        {
            return NotFound("Utilisateur non trouvé: " + unique_name);
        }

        if (customer.AcceptTermsAt != null)
        {
            return BadRequest("Les conditions d'utilisation ont déjà été acceptées.");
        }

        customer.AcceptTermsAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(token);

        await _cache.RemoveAsync($"Customer_{customer.UniqueName}", token);

        return NoContent();
    }

    /// <summary>
    /// Permet de lister les utilisateurs en exposant un minimum d'information.
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [EnableQuery(MaxExpansionDepth = 0)]
    [ProducesResponseType(200, Type = typeof(List<GetCustomer>))]
    public async Task<IActionResult> GetCustomers(CancellationToken token)
    {
        var customers = await _context.Customers.ProjectTo<GetCustomer>(_mapper.ConfigurationProvider)
                                                .ToListAsync(token);

        // Masquer avec des * certains caractères des adresses courriel
        foreach (var customer in customers.Where(c => !string.IsNullOrEmpty(c.Email) && c.Email.Contains('@')))
        {
            if (customer.Email == null)
            {
                continue;
            }

            var email = customer.Email.Split('@');
            var name = email[0];
            var domain = email[1];

            var nameLength = name.Length;
            var nameToHide = nameLength / 2;
            var nameStart = name.Substring(0, nameToHide);
            var nameEnd = name.Substring(nameToHide, nameLength - nameToHide);

            customer.Email = $"{nameStart}{new string('*', nameEnd.Length)}@{domain}";
        }

        return Ok(customers);
    }

    /// <summary>
    /// Point de terminaison pour l'administration des utilisateurs
    /// </summary>
    /// <returns></returns>
    [HttpGet("/Admin/Customers")]
    [EnableQuery]
    [Authorize(Roles = "administrateur", Policy = "TenantIdPrincipal")]
    public IQueryable<Customer> GetCustomersAdmin()
    {
        return _context.Customers;
    }

    /// <summary>
    /// Permet a un administrateur de consentir aux termes d'utilisation pour un appareil spécifique.
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    [HttpPost("/Admin/Customer/ConsentForDevice/{deviceId}")]
    [Authorize(Roles = "administrateur", Policy = "TenantIdPrincipal")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> ConsentTermeForDevice(Guid deviceId, CancellationToken token)
    {
        var device = await _context.Customers.FirstOrDefaultAsync(c => c.UniqueName == deviceId.ToString(), token);

        if (device == null)
        {
            return NotFound("L'appareil n'a pas été trouvé.");
        }

        if (device.AcceptTermsAt != null)
        {
            return BadRequest("Les conditions d'utilisation ont déjà été acceptées pour cet appareil.");
        }

        device.AcceptTermsAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(token);

        await _cache.RemoveAsync($"Customer_{device.UniqueName}", token);

        return NoContent();
    }

    /// <summary>
    /// Point de terminaison pour modifier un utilisateur
    /// </summary>
    /// <returns></returns>
    [HttpPut]
    [Route("/Admin/Customers/{id}")]
    [ProducesResponseType(200, Type = typeof(GetCustomer))]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [Authorize(Roles = "administrateur", Policy = "TenantIdPrincipal")]
    public async Task<IActionResult> PutCustomer(Guid id, PutCustomer putCustomer, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(putCustomer.Name))
        {
            return BadRequest("Le nom ne doit pas être vide");
        }
        if (id != putCustomer.Id)
        {
            return BadRequest("L'id de l'utilisateur dans la route ne concorde pas avec l'id dans le corps du message.");
        }

        var entity = await _context.Customers.FindAsync([id], token);

        if (entity != null && entity.Id == id)
        {
            entity.Name = putCustomer.Name;

            await _context.SaveChangesAsync(token);

            await _cache.RemoveAsync($"Customer_{entity.UniqueName}", token);

            return Ok(entity);
        }
        else
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Point de terminaison pour la suppression d'un utilisateur
    /// </summary>
    /// <param name="id">Id de l'utilisateur</param>
    /// <param name="token"></param>
    /// <returns></returns>
    [HttpDelete]
    [Route("/admin/customers/{id}")]
    [Authorize(Roles = "administrateur", Policy = "TenantIdPrincipal")]
    public async Task<IActionResult> DeleteCustomerAdmin(Guid id, CancellationToken token)
    {
        var customer = await _context.Customers
            .Include(c => c.CustomerErablieres)
            .FirstOrDefaultAsync(c => c.Id == id, token);

        if (customer == null)
        {
            return NoContent();
        }

        _context.Remove(customer);

        await _context.SaveChangesAsync(token);

        await _cache.RemoveAsync($"Customer_{customer.UniqueName}", token);

        return NoContent();
    }

    /// <summary>
    /// Point de terminaison d'administration pour 
    /// récupérer les accès d'un utilisateur
    /// </summary>
    /// <param name="id">Id de l'utilisateur</param>
    /// <param name="token"></param>
    /// <returns></returns>
    [HttpGet]
    [Route("/Admin/Customers/{id}/Access")]
    [Authorize(Roles = "administrateur", Policy = "TenantIdPrincipal")]
    [ProducesResponseType(200, Type = typeof(GetCustomerAccess))]
    public async Task<IActionResult> GetAdminCustomerAccess(Guid id, CancellationToken token)
    {
        var customer = await _context.Customers.FindAsync([id], cancellationToken: token);

        if (customer == null)
        {
            return NotFound();
        }

        var erablieres = await _context.CustomerErablieres.AsNoTracking()
            .Where(c => c.IdCustomer == id)
            .ProjectTo<GetCustomerAccess>(_mapper.ConfigurationProvider)
            .ToArrayAsync(token);

        return Ok(erablieres);
    }

}
