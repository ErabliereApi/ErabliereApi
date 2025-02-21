using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using System.Net.Http.Headers;
using static Microsoft.Graph.CoreConstants;

namespace ErabliereApi.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class QuantumController : ControllerBase
{
    private readonly IConfiguration _config;

    public QuantumController(IConfiguration config)
    {
        _config = config;
    }

    [HttpGet("[action]")]
    public async Task<IActionResult> GetJobs(CancellationToken token)
    {
        string url = "https://api.quantum-computing.ibm.com/runtime/jobs?limit=10&offset=0&exclude_params=true";

        return await IbmQuantumQuery(url, token);
    }

    [HttpGet("[action]")]
    public async Task<IActionResult> GetBackends(CancellationToken token)
    {
        string url = "https://api.quantum-computing.ibm.com/runtime/backends";

        return await IbmQuantumQuery(url, token);
    }

    private async Task<IActionResult> IbmQuantumQuery(string url, CancellationToken token)
    {
        HttpClient client = new HttpClient();

        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _config["IQP_API_TOKEN"]);

        HttpResponseMessage response = await client.GetAsync(url, token);

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
