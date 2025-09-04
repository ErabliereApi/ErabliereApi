using ErabliereApi.Depot.Sql;
using ErabliereApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;

namespace ErabliereApi.Controllers;

/// <summary>
/// Contrôleur pour gérer les informations IP
/// </summary>
[ApiController]
[Route("ipinfo")]
[Authorize(Roles = "administrateur", Policy = "TenantIdPrincipal")]
public class IpInfoController : ControllerBase
{
    private readonly ErabliereDbContext _context;

    /// <summary>
    /// Constructeur
    /// </summary>
    public IpInfoController(ErabliereDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Récupère les informations IP stockées dans la base de données
    /// </summary>
    /// <returns>Liste des informations IP</returns>
    [HttpGet]
    [EnableQuery]
    public IQueryable<IpInfo> GetIpInfo()
    {
        return _context.IpInfos.AsNoTracking();
    }
}