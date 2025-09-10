using ErabliereApi.Attributes;
using ErabliereApi.Depot.Sql;
using ErabliereApi.Services;
using ErabliereApi.Services.AccuWeatherModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErabliereApi.Controllers;

/// <summary>
/// Contrôler pour interagir avec les alertes des capteurs
/// </summary>
[ApiController]
[Route("Erablieres/{id}/[controller]")]
[Authorize]
public class WeatherForecastController : ControllerBase
{
    private readonly ErabliereDbContext _context;
    private readonly WeatherService _weatherService;

    /// <summary>
    /// Constructeur
    /// </summary>
    public WeatherForecastController(ErabliereDbContext context, WeatherService weatherService)
    {
        _context = context;
        _weatherService = weatherService;
    }

    /// <summary>
    /// Obtenir les prévisions météo pour une érablière
    /// </summary>
    /// <param name="id">Identifiant de l'érablière</param>
    /// <param name="lang">Paramètre de langue, fr-ca par défaut.</param>
    /// <param name="token">Token d'annulation</param>
    /// <returns>Prévisions météo</returns>
    /// <response code="200">Prévisions météo</response>
    /// <response code="401">Non autorisé</response>
    /// <response code="404">Érablière non trouvée</response>
    /// <response code="500">Erreur interne du serveur</response>
    [HttpGet]
    [ProducesResponseType(200, Type = typeof(WeatherForecastResponse))]
    [AllowAnonymous]
    [ValiderOwnership("id")]
    public async Task<IActionResult> GetWeatherForecast(Guid id, CancellationToken token, string lang = "fr-ca")
    {
        // Résoudre l'érablière
        var erabliere = await _context.Erabliere.FindAsync([id], token);

        // Vérifier si l'érablière existe
        if (erabliere == null)
        {
            return new NotFoundResult();
        }

        if (string.IsNullOrWhiteSpace(erabliere.CodePostal))
        {
            ModelState.AddModelError("id", "L'érablière n'a pas de code postal");

            return new BadRequestObjectResult(new ValidationProblemDetails(ModelState));
        }

        var (code, locationCode) = await _weatherService.GetLocationCodeAsync(erabliere.CodePostal, token);

        if (code != 200)
        {
            ModelState.AddModelError("id", "Impossible de trouver le code de localisation.");

            return new BadRequestObjectResult(new ValidationProblemDetails(ModelState));
        }

        var weatherForecast = await _weatherService.GetWeatherForecastAsync(locationCode, lang, token);

        if (weatherForecast == null)
        {
            ModelState.AddModelError("id", "Impossible de trouver les prévisions météo");

            return new BadRequestObjectResult(new ValidationProblemDetails(ModelState));
        }

        return new OkObjectResult(weatherForecast);
    }

    /// <summary>
    /// Obtenir les prévisions météo pour les prochaines heures
    /// </summary>
    /// <param name="id">Identifiant de l'érablière</param>
    /// <param name="lang">Paramètre de langue, fr-ca par défaut.</param>
    /// <param name="token">Token d'annulation</param>
    /// <returns>Prévisions météo</returns>
    /// <response code="200">Prévisions météo</response>
    /// <response code="401">Non autorisé</response>
    /// <response code="404">Érablière non trouvée</response>
    [HttpGet("Hourly")]
    [ProducesResponseType(200, Type = typeof(HourlyWeatherForecastResponse[]))]
    [AllowAnonymous]
    [ValiderOwnership("id")]
    public async Task<IActionResult> GetHourlyWeatherForecast(Guid id, CancellationToken token, string lang = "fr-ca")
    {
        // Résoudre l'érablière
        var erabliere = await _context.Erabliere.FindAsync([id], token);

        // Vérifier si l'érablière existe
        if (erabliere == null)
        {
            return new NotFoundResult();
        }

        if (string.IsNullOrWhiteSpace(erabliere.CodePostal))
        {
            ModelState.AddModelError("id", "L'érablière n'a pas de code postal");

            return new BadRequestObjectResult(new ValidationProblemDetails(ModelState));
        }

        var (code, locationCode) = await _weatherService.GetLocationCodeAsync(erabliere.CodePostal, token);

        if (code != 200)
        {
            ModelState.AddModelError("id", "Impossible de trouver le code de localisation.");

            return new BadRequestObjectResult(new ValidationProblemDetails(ModelState));
        }

        var weatherForecast = await _weatherService.GetHourlyForecastAsync(locationCode, lang, token);

        if (weatherForecast == null)
        {
            ModelState.AddModelError("id", "Impossible de trouver les prévisions météo");

            return new BadRequestObjectResult(new ValidationProblemDetails(ModelState));
        }

        return new OkObjectResult(weatherForecast);
    }
}
