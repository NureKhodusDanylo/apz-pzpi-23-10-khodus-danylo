using Application.Abstractions.Interfaces;
using Application.DTOs.FriendshipDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RobDeliveryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FriendshipController : ControllerBase
    {
        private readonly IFriendshipService _friendshipService;

        public FriendshipController(IFriendshipService friendshipService)
        {
            _friendshipService = friendshipService;
        }

        [HttpPost("request")]
        public async Task<IActionResult> SendRequest([FromBody] SendFriendRequestDTO request)
        {
            var userIdClaim = User.FindFirst("Id")?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            try
            {
                await _friendshipService.SendFriendRequestAsync(userId, request);
                return Ok(new { message = "Friend request sent successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("request/{requestId}/accept")]
        public async Task<IActionResult> AcceptRequest(int requestId)
        {
            var userIdClaim = User.FindFirst("Id")?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            try
            {
                await _friendshipService.AcceptFriendRequestAsync(userId, requestId);
                return Ok(new { message = "Friend request accepted" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("request/{requestId}/reject")]
        public async Task<IActionResult> RejectRequest(int requestId)
        {
            var userIdClaim = User.FindFirst("Id")?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            try
            {
                await _friendshipService.RejectFriendRequestAsync(userId, requestId);
                return Ok(new { message = "Friend request rejected" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetFriends()
        {
            var userIdClaim = User.FindFirst("Id")?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var friends = await _friendshipService.GetFriendsAsync(userId);
            return Ok(friends);
        }

        [HttpGet("requests/pending")]
        public async Task<IActionResult> GetPendingRequests()
        {
            var userIdClaim = User.FindFirst("Id")?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var requests = await _friendshipService.GetPendingRequestsAsync(userId);
            return Ok(requests);
        }

        [HttpDelete("{friendId}")]
        public async Task<IActionResult> RemoveFriend(int friendId)
        {
            var userIdClaim = User.FindFirst("Id")?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            try
            {
                await _friendshipService.RemoveFriendAsync(userId, friendId);
                return Ok(new { message = "Friend removed" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
