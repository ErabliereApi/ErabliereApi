using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace ErabliereApi.Controllers;

/// <summary>
/// Controlleur pour les requêtes vers le service IBM Quantum.
/// </summary>
[ApiController]
[Route("[controller]")]
[Authorize]
public class QuantumController : ControllerBase
{
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
    /// <param name="token"></param>
    /// <returns></returns>
    [HttpGet("[action]")]
    public async Task<IActionResult> GetJobs(CancellationToken token)
    {
        string path = "/runtime/jobs?limit=10&offset=0&exclude_params=true";

        return await IbmQuantumQuery(path, token);
    }

    /// <summary>
    /// Retourne les backends.
    /// </summary>
    [HttpGet("[action]")]
    public async Task<IActionResult> GetBackends(CancellationToken token)
    {
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
