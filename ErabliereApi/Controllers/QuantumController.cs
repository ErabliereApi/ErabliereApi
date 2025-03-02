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
    private readonly IConfiguration _config;
    private readonly string _baseUrl;

    /// <summary>
    /// Constructeur.
    /// </summary>
    public QuantumController(IConfiguration config)
    {
        _config = config;
        _baseUrl = "https://api.quantum-computing.ibm.com";
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
        HttpClient client = new HttpClient();

        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _config["IQP_API_TOKEN"]);

        HttpResponseMessage response = await client.GetAsync($"{_baseUrl}{path}", token);

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
