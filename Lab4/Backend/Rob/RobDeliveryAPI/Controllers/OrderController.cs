using Application.Abstractions.Interfaces;
using Application.DTOs.OrderDTOs;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RobDeliveryAPI.Extensions;
using System.Security.Claims;
using Application.Abstractions.Exceptions;

namespace RobDeliveryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IFileService _fileService;
        private readonly IWebHostEnvironment _environment;

        public OrderController(
            IOrderService orderService,
            IFileService fileService,
            IWebHostEnvironment environment)
        {
            _orderService = orderService;
            _fileService = fileService;
            _environment = environment;
        }

        /// <summary>
        /// Get authenticated user ID from JWT token
        /// </summary>
        private int GetAuthenticatedUserId()
        {
            var userIdClaim = User.FindFirst("Id") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                throw new UnauthorizedAccessException("User ID not found in token");
            }
            return userId;
        }

        /// <summary>
        /// Create a new order with optional images (uses authenticated user from JWT token)
        /// </summary>
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateOrder(
            [FromForm] string name,
            [FromForm] string description,
            [FromForm] double weight,
            [FromForm] decimal productPrice,
            [FromForm] bool isProductPaid,
            [FromForm] int recipientId,
            [FromForm] int deliveryPayer,
            IFormFileCollection? files = null)
        {
            try
            {
                int senderId = GetAuthenticatedUserId();

                // Create order DTO
                var orderDto = new CreateOrderDTO
                {
                    Name = name,
                    Description = description,
                    Weight = weight,
                    ProductPrice = productPrice,
                    IsProductPaid = isProductPaid,
                    RecipientId = recipientId,
                    DeliveryPayer = (Entities.Models.DeliveryPayer)deliveryPayer
                };

                // Create order
                var result = await _orderService.CreateOrderAsync(senderId, orderDto);

                // Upload files if provided
                if (files != null && files.Count > 0)
                {
                    await _fileService.UploadOrderImagesAsync(result.Id, files.ToFileUploadDTOs(), _environment.ContentRootPath);

                    // Reload order with images
                    result = await _orderService.GetOrderByIdAsync(result.Id);
                }

                return CreatedAtAction(nameof(GetOrderById), new { id = result.Id }, result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while creating the order", details = ex.Message });
            }
        }

        /// <summary>
        /// Estimate delivery price based on weight
        /// </summary>
        [HttpGet("estimate-price")]
        public async Task<IActionResult> EstimateDeliveryPrice([FromQuery] double weight)
        {
            if (weight <= 0)
            {
                return BadRequest(new { error = "Weight must be greater than zero" });
            }
            
            var price = await _orderService.CalculateDeliveryPriceAsync(weight);
            return Ok(new { deliveryPrice = price });
        }

        /// <summary>
        /// Get order by ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            try
            {
                var order = await _orderService.GetOrderByIdAsync(id);
                if (order == null)
                {
                    return NotFound(new { error = "Order not found" });
                }
                return Ok(order);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving the order", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all orders
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllOrders()
        {
            try
            {
                var orders = await _orderService.GetAllOrdersAsync();
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving orders", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all orders for authenticated user (sent and received)
        /// </summary>
        [HttpGet("my-orders")]
        public async Task<IActionResult> GetMyOrders()
        {
            try
            {
                int userId = GetAuthenticatedUserId();
                var orders = await _orderService.GetUserOrdersAsync(userId);
                return Ok(orders);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving user orders", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all orders for a specific user (sent and received) - Admin only
        /// </summary>
        [HttpGet("user/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUserOrders(int userId)
        {
            try
            {
                var orders = await _orderService.GetUserOrdersAsync(userId);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving user orders", details = ex.Message });
            }
        }

        /// <summary>
        /// Get orders sent by a specific user
        /// </summary>
        [HttpGet("sent/{senderId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetSentOrders(int senderId)
        {
            try
            {
                var orders = await _orderService.GetSentOrdersAsync(senderId);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving sent orders", details = ex.Message });
            }
        }

        /// <summary>
        /// Get orders received by a specific user
        /// </summary>
        [HttpGet("received/{recipientId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetReceivedOrders(int recipientId)
        {
            try
            {
                var orders = await _orderService.GetReceivedOrdersAsync(recipientId);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving received orders", details = ex.Message });
            }
        }

        /// <summary>
        /// Get orders by status
        /// </summary>
        [HttpGet("status/{status}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetOrdersByStatus(OrderStatus status)
        {
            try
            {
                var orders = await _orderService.GetOrdersByStatusAsync(status);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving orders by status", details = ex.Message });
            }
        }

        /// <summary>
        /// Update order status
        /// </summary>
        [HttpPatch]
        public async Task<IActionResult> UpdateOrderStatus([FromBody] UpdateOrderStatusDTO updateDto)
        {
            try
            {
                var result = await _orderService.UpdateOrderStatusAsync(updateDto.OrderId, updateDto.NewStatus);
                if (!result)
                {
                    return NotFound(new { error = "Order not found" });
                }
                return Ok(new { message = "Order status updated successfully" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating order status", details = ex.Message });
            }
        }

        /// <summary>
        /// Assign a robot to an order
        /// </summary>
        [HttpPost("{id}/assign-robot")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignRobotToOrder(int id, [FromBody] AssignRobotDTO assignDto)
        {
            try
            {
                var result = await _orderService.AssignRobotToOrderAsync(id, assignDto.RobotId);
                if (!result)
                {
                    return NotFound(new { error = "Order not found" });
                }
                return Ok(new { message = "Robot assigned successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while assigning robot", details = ex.Message });
            }
        }

        /// <summary>
        /// Cancel an order (only order sender can cancel)
        /// </summary>
        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelOrder(int id)
        {
            try
            {
                int userId = GetAuthenticatedUserId();
                var result = await _orderService.CancelOrderAsync(id, userId);
                if (!result)
                {
                    return NotFound(new { error = "Order not found" });
                }
                return Ok(new { message = "Order cancelled successfully" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while cancelling the order", details = ex.Message });
            }
        }

        /// <summary>
        /// Delete an order
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            try
            {
                var result = await _orderService.DeleteOrderAsync(id);
                if (!result)
                {
                    return NotFound(new { error = "Order not found" });
                }
                return Ok(new { message = "Order deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while deleting the order", details = ex.Message });
            }
        }

        /// <summary>
        /// Execute an order - automatically find and assign optimal drone with route calculation
        /// </summary>
        [HttpPost("{id}/execute")]
        public async Task<IActionResult> ExecuteOrder(int id)
        {
            try
            {
                int userId = GetAuthenticatedUserId();
                var result = await _orderService.ExecuteOrderAsync(id, userId);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (RoutingException ex)
            {
                return BadRequest(new { 
                    error = ex.Message, 
                    debugLogs = ex.DebugLogs 
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while executing the order", details = ex.Message });
            }
        }
    }
}
