using Application.Abstractions.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RobDeliveryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MapController : ControllerBase
    {
        private readonly IMapService _mapService;

        public MapController(IMapService mapService)
        {
            _mapService = mapService;
        }

        /// <summary>
        /// Get all map data (robots and nodes positions)
        /// </summary>
        [HttpGet("data")]
        public async Task<IActionResult> GetMapData()
        {
            try
            {
                int? userId = null;
                var userIdClaim = User.FindFirst("Id")?.Value;
                if (userIdClaim != null && int.TryParse(userIdClaim, out int parsedId))
                {
                    userId = parsedId;
                }

                var mapData = await _mapService.GetMapDataAsync(userId);
                return Ok(mapData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving map data", details = ex.Message });
            }
        }
    }
}
