using Application.Abstractions.Interfaces;
using Entities.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace RobDeliveryAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class WalletController : ControllerBase
{
    private readonly IUserRepository _userRepository;

    public WalletController(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    private int GetAuthenticatedUserId()
    {
        var userIdClaim = User.FindFirst("Id") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }

    [HttpGet("balance")]
    public async Task<ActionResult<object>> GetBalance()
    {
        try
        {
            int userId = GetAuthenticatedUserId();
            var user = await _userRepository.GetByIdAsync(userId);
            
            if (user == null)
            {
                return NotFound(new { success = false, message = "User not found" });
            }

            return Ok(new { success = true, balance = user.Balance });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
        }
    }

    [HttpPost("withdraw")]
    public async Task<ActionResult<object>> Withdraw()
    {
        try
        {
            int userId = GetAuthenticatedUserId();
            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null)
            {
                return NotFound(new { success = false, message = "User not found" });
            }

            if (user.Balance <= 0)
            {
                return BadRequest(new { success = false, message = "No funds to withdraw" });
            }

            decimal withdrawnAmount = user.Balance;
            // Simulate Payout - just reset to 0
            user.Balance = 0;
            await _userRepository.UpdateAsync(user);

            return Ok(new
            {
                success = true,
                message = "Withdrawal successful",
                withdrawnAmount = withdrawnAmount,
                newBalance = user.Balance,
                processedAt = DateTime.UtcNow
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
        }
    }
}
