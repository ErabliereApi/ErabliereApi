using ErabliereApi.Donnees.Action.Get;
using Microsoft.AspNetCore.Mvc;

namespace ErabliereApi.Controllers
{
    /// <summary>
    /// Controller to get access token for map services
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class MapController : ControllerBase
    {
        private readonly IConfiguration _config;

        /// <summary>
        /// Constructor
        /// </summary>
        public MapController(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// Get access token for a map service
        /// </summary>
        [HttpGet("access-token/{provider}")]
        [ProducesResponseType(200, Type = typeof(GetMapAccessToken))]
        public IActionResult GetAccessToken([FromRoute] string provider)
        {
            if (provider == "mapbox")
            {
                return Ok(new { AccessToken = _config["Mapbox_AccessToken"] });
            }

            ModelState.AddModelError("provider", "Provider not supported");

            return BadRequest(new ValidationProblemDetails(ModelState));
        }
    }
}