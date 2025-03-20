using ErabliereApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErabliereApi.Controllers;

/// <summary>
/// Controller to get access token for map services
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "administrateur", Policy = "TenantIdPrincipal")]
public class HologramController : ControllerBase
{
    private readonly IConfiguration _config;

    /// <summary>
    /// Constructor
    /// </summary>
    public HologramController(IConfiguration config)
    {
        _config = config;
    }

    /// <summary>
    /// Get access token for a map service
    /// </summary>
    [HttpGet()]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetTunnelKeys(CancellationToken token)
    {
        var hologramConfig = _config.GetValue<string>("Hologram_Token");

        using var client = new HttpClient();

        var response = await client.GetAsync($"https://dashboard.hologram.io/api/1/tunnelkeys?apikey={hologramConfig}", token);

        var obj = await response.Content.ReadFromJsonAsync<object?>();

        return Ok(obj);
    }

    // TODO: https://support.hologram.io/hc/en-us/articles/360035696793-Enable-and-disable-device-tunneling-keys-using-the-REST-API
    // https://hologram.docs.apiary.io/#introduction/authentication/header-example
}
