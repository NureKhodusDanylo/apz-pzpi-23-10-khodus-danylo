using Application.Abstractions.Interfaces;
using Application.DTOs.NodeDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RobDeliveryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NodeController : ControllerBase
    {
        private readonly INodeService _nodeService;

        public NodeController(INodeService nodeService)
        {
            _nodeService = nodeService;
        }

        /// <summary>
        /// Create a new delivery node
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateNode([FromBody] CreateNodeDTO nodeDto)
        {
            try
            {
                var result = await _nodeService.CreateNodeAsync(nodeDto);
                return CreatedAtAction(nameof(GetNodeById), new { id = result.Id }, result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while creating the node", details = ex.Message });
            }
        }

        /// <summary>
        /// Get node by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetNodeById(int id)
        {
            try
            {
                var node = await _nodeService.GetNodeByIdAsync(id);
                if (node == null)
                {
                    return NotFound(new { error = "Node not found" });
                }
                return Ok(node);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving the node", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all nodes
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllNodes()
        {
            try
            {
                var nodes = await _nodeService.GetAllNodesAsync();
                return Ok(nodes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving nodes", details = ex.Message });
            }
        }

        /// <summary>
        /// Get nodes by type
        /// </summary>
        [HttpGet("type/{type}")]
        public async Task<IActionResult> GetNodesByType(NodeType type)
        {
            try
            {
                var nodes = await _nodeService.GetNodesByTypeAsync(type);
                return Ok(nodes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving nodes by type", details = ex.Message });
            }
        }

        /// <summary>
        /// Find nearest node to given coordinates
        /// </summary>
        [HttpGet("nearest")]
        public async Task<IActionResult> FindNearestNode([FromQuery] double latitude, [FromQuery] double longitude, [FromQuery] NodeType? type = null)
        {
            try
            {
                var node = await _nodeService.FindNearestNodeAsync(latitude, longitude, type);
                if (node == null)
                {
                    return NotFound(new { error = "No nodes found" });
                }
                return Ok(node);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while finding nearest node", details = ex.Message });
            }
        }

        /// <summary>
        /// Update a node
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateNode(int id, [FromBody] UpdateNodeDTO nodeDto)
        {
            try
            {
                if (id != nodeDto.Id)
                {
                    return BadRequest(new { error = "ID mismatch" });
                }

                var result = await _nodeService.UpdateNodeAsync(nodeDto);
                if (!result)
                {
                    return NotFound(new { error = "Node not found" });
                }

                return Ok(new { message = "Node updated successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating the node", details = ex.Message });
            }
        }

        /// <summary>
        /// Delete a node
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNode(int id)
        {
            try
            {
                var result = await _nodeService.DeleteNodeAsync(id);
                if (!result)
                {
                    return NotFound(new { error = "Node not found" });
                }

                return Ok(new { message = "Node deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while deleting the node", details = ex.Message });
            }
        }
    }
}
