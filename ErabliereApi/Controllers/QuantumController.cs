using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErabliereApi.Controllers;

/// <summary>
/// Controlleur pour les requêtes vers le service IBM Quantum.
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize]
public class QuantumController : ControllerBase
{
    private static readonly string[] _validProviders = ["IBM", "Quandela"];
    private readonly IHttpClientFactory _factory;

    /// <summary>
    /// Constructeur.
    /// </summary>
    public QuantumController(IHttpClientFactory httpClientFactory)
    {
        _factory = httpClientFactory;
    }

    /// <summary>
    /// Retourne les jobs.
    /// </summary>
    /// <param name="provider">Name of the provider (IBM or Quandela)</param>
    /// <param name="token"></param>
    /// <returns></returns>
    [HttpGet("[action]")]
    public async Task<IActionResult> GetJobs([FromQuery] string provider, CancellationToken token)
    {
        if (!_validProviders.Contains(provider))
        {
            ModelState.AddModelError("provider", $"Query string parameter provider must be one of [{_validProviders.Aggregate((a, b) => string.Concat(a, ',', b))}]");

            return BadRequest(new ValidationProblemDetails(ModelState));
        }

        string path = "/runtime/jobs?limit=10&offset=0&exclude_params=true";

        return await IbmQuantumQuery(path, token);
    }

    /// <summary>
    /// Retourne les backends.
    /// </summary>
    [HttpGet("[action]")]
    public async Task<IActionResult> GetBackends([FromQuery] string provider, CancellationToken token)
    {
        if (!_validProviders.Contains(provider))
        {
            ModelState.AddModelError("provider", $"Query string parameter provider must be one of [{_validProviders.Aggregate((a, b) => string.Concat(a, ',', b))}]");

            return BadRequest(new ValidationProblemDetails(ModelState));
        }

        string path = "/runtime/backends";

        return await IbmQuantumQuery(path, token);
    }

    private async Task<IActionResult> IbmQuantumQuery(string path, CancellationToken token)
    {
        var client = _factory.CreateClient("IbmQuantumClient");

        HttpResponseMessage response = await client.GetAsync(path, token);

        if (response.IsSuccessStatusCode)
        {
            return Ok(await response.Content.ReadFromJsonAsync(typeof(object), token));
        }
        else
        {
            return BadRequest(await response.Content.ReadFromJsonAsync(typeof(object), token));
        }
    }
}
