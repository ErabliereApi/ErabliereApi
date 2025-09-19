using System.Net;
using ErabliereApi.Attributes;
using ErabliereApi.Donnees.Action.Get;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErabliereApi.Controllers;

/// <summary>
/// Contrôler représentant les données des dompeux
/// </summary>
[ApiController]
[Route("Erablieres/{id}/[controller]")]
[Authorize]
public class ImagesCapteurController : ControllerBase
{
    private readonly IHttpClientFactory _httpClietFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ImagesCapteurController> _logger;

    /// <summary>
    /// Constructeur par initialisation
    /// </summary>
    public ImagesCapteurController(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<ImagesCapteurController> logger)
    {
        _httpClietFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Liste les données d'un capteur
    /// </summary>
    /// <param name="id">Identifiant de l'érablière</param>
    /// <param name="take">Nombre de données à prendre</param>
    /// <param name="skip">Nombre de données à sauter</param>
    /// <param name="search">Recherche</param>
    /// <param name="token">Token d'annulation</param>
    /// <response code="200">Une liste de DonneesCapteur.</response>
    [HttpGet]
    [ValiderOwnership("id")]
    [ProducesResponseType(200, Type = typeof(IEnumerable<GetImageInfo>))]
    public async Task<IActionResult> Lister([FromRoute] Guid? id,
                                            [FromQuery] int? take,
                                            [FromQuery] int? skip,
                                            [FromQuery] string? search,
                                                        CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(_configuration["EmailImageObserverUrl"]))
        {
            _logger.LogInformation("L'URL de l'observateur d'images n'est pas configurée. Une liste vide sera retournée.");

            return Ok(new List<GetImageInfo>());
        }

        using var client = _httpClietFactory.CreateClient("EmailImageObserver");

        string route = $"/api/image?ownerId={id}";

        if (take.HasValue)
        {
            route += $"&take={take}";
        }

        if (skip.HasValue)
        {
            route += $"&skip={skip}";
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            route += $"&search={search}";
        }

        try
        {
            var response = await client.GetAsync(route, token);

            var obj = await response.Content.ReadFromJsonAsync<List<GetImageInfo>>(token);

            return Ok(obj);
        }
        catch (HttpRequestException e) 
        {
            return StatusCode((int)(e.StatusCode ?? HttpStatusCode.InternalServerError), e.Message);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
}
