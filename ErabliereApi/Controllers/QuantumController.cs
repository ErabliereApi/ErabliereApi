using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ErabliereApi.Controllers;

/// <summary>
/// Controlleur pour les requêtes vers le service IBM Quantum.
/// </summary>
/// <remarks>
/// Quandela API reference: https://hub.quandela.com/integrate
/// </remarks>
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
    /// <param name="provider">Name of the provider (IBM)</param>
    /// <param name="token"></param>
    /// <returns></returns>
    [HttpGet("[action]")]
    public async Task<IActionResult> GetJobs([FromQuery] string provider, CancellationToken token)
    {
        string path = "";

        switch (provider)
        {
            case "IBM":
                path = "/runtime/jobs?limit=10&offset=0&exclude_params=true";

                return await IbmQuantumQuery(path, token);
            default:
                ModelState.AddModelError("provider", $"Query string parameter provider must be IBM");

                return BadRequest(new ValidationProblemDetails(ModelState));
        }
    }

    /// <summary>
    /// Get detail of a job (IBM or Quandela)
    /// </summary>
    /// <param name="id"></param>
    /// <param name="provider"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    [HttpGet("[action]/{id}")]
    public async Task<IActionResult> GetJobs([FromRoute] string id, [FromQuery] string provider, CancellationToken token)
    {
        string path = "";

        switch (provider)
        {
            case "IBM":
                path = $"/runtime/jobs/{id}";

                return await IbmQuantumQuery(path, token);
            case "Quandela":
                path = $"/api/jobs/{id}/data";

                return await QuandelaQuery(path, token);
            default:
                ModelState.AddModelError("provider", $"Query string parameter provider must be on of {JsonSerializer.Serialize(_validProviders)}");

                return BadRequest(new ValidationProblemDetails(ModelState));
        }
    }

    /// <summary>
    /// Retourne les backends.
    /// </summary>
    [HttpGet("[action]")]
    public async Task<IActionResult> GetBackends([FromQuery] string provider, CancellationToken token)
    {
        string path = "";

        switch (provider)
        {
            case "IBM":
                path = "/runtime/backends";

                return await IbmQuantumQuery(path, token);
            default:
                ModelState.AddModelError("provider", $"Query string parameter provider must be IBM");

                return BadRequest(new ValidationProblemDetails(ModelState));
        }
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

    private async Task<IActionResult> QuandelaQuery(string path, CancellationToken token)
    {
        var client = _factory.CreateClient("QuandelaClient");

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
