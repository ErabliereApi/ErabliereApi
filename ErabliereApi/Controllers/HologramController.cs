using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErabliereApi.Controllers;

/// <summary>
/// Controller to get access token for map services
/// 
/// Documentation supplémentaire:
/// https://support.hologram.io/hc/en-us/articles/360035696793-Enable-and-disable-device-tunneling-keys-using-the-REST-API
/// https://hologram.docs.apiary.io/#introduction/authentication/header-example
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "administrateur", Policy = "TenantIdPrincipal")]
public class HologramController : ControllerBase
{
    private readonly IHttpClientFactory _factory;

    /// <summary>
    /// Constructor
    /// </summary>
    public HologramController(IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _factory = httpClientFactory;
    }

    /// <summary>
    /// Get access token for a map service
    /// </summary>
    [HttpGet("[action]")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetTunnelKeys(CancellationToken token)
    {
        var client = _factory.CreateClient("HologramClient");

        var response = await client.GetAsync("/api/1/tunnelkeys", token);

        var obj = await response.Content.ReadFromJsonAsync<object?>();

        return Ok(obj);
    }

    /// <summary>
    /// Enable a tunnel key
    /// </summary>
    [HttpPut("{id}/[action]")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Enable(int id, CancellationToken token)
    {
        var client = _factory.CreateClient("HologramClient");

        var response = await client.PostAsync($"/api/1/tunnelkeys/{id}/enable", null, token);

        response.EnsureSuccessStatusCode();

        var obj = await response.Content.ReadFromJsonAsync<object?>();

        return Ok(obj);
    }

    /// <summary>
    /// Enable a tunnel key
    /// </summary>
    [HttpPut("{id}/[action]")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Disable(int id, CancellationToken token)
    {
        var client = _factory.CreateClient("HologramClient");

        var response = await client.PostAsync($"/api/1/tunnelkeys/{id}/disable", null, token);

        response.EnsureSuccessStatusCode();

        var obj = await response.Content.ReadFromJsonAsync<object?>();

        return Ok(obj);
    }
}
