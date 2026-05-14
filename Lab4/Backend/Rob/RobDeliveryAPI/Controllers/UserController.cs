using Application.Abstractions.Interfaces;
using Application.DTOs.UserDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RobDeliveryAPI.Extensions;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RobDeliveryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IWebHostEnvironment _environment;

        public UserController(
            IUserService userService,
            IWebHostEnvironment environment)
        {
            _userService = userService;
            _environment = environment;
        }

        /// <summary>
        /// Get full profile of authenticated user
        /// </summary>
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userIdClaim = User.FindFirst("Id")?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            try
            {
                var profile = await _userService.GetProfileAsync(userId);
                if (profile == null)
                {
                    return NotFound(new { error = "User not found" });
                }

                return Ok(profile);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving profile", details = ex.Message });
            }
        }

        /// <summary>
        /// Get profile photo of authenticated user
        /// </summary>
        [HttpGet("profile/photo")]
        public async Task<IActionResult> GetProfilePhoto()
        {
            var userIdClaim = User.FindFirst("Id")?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            try
            {
                var photo = await _userService.GetProfilePhotoAsync(userId);
                if (photo == null)
                {
                    return NotFound(new { error = "Profile photo not found" });
                }

                var fileBytes = await _userService.GetProfilePhotoContentAsync(userId, _environment.ContentRootPath);
                if (fileBytes == null)
                {
                    return NotFound(new { error = "Profile photo file not found on disk" });
                }

                return File(fileBytes, photo.ContentType, photo.FileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving profile photo", details = ex.Message });
            }
        }

        /// <summary>
        /// Get profile photo of specific user by ID
        /// </summary>
        [HttpGet("{userId}/photo")]
        [Authorize]
        public async Task<IActionResult> GetUserPhoto(int userId)
        {
            try
            {
                var photo = await _userService.GetProfilePhotoAsync(userId);
                if (photo == null)
                {
                    return NotFound(new { error = "Profile photo not found" });
                }

                var fileBytes = await _userService.GetProfilePhotoContentAsync(userId, _environment.ContentRootPath);
                if (fileBytes == null)
                {
                    return NotFound(new { error = "Profile photo file not found on disk" });
                }

                return File(fileBytes, photo.ContentType, photo.FileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving profile photo", details = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var userDto = await _userService.GetUserByIdAsync(id);
            if (userDto == null)
            {
                return NotFound(new { error = "User not found" });
            }

            return Ok(userDto);
        }

        [HttpGet("my-node")]
        public async Task<IActionResult> GetMyNode()
        {
            var userIdClaim = User.FindFirst("Id")?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            try
            {
                var nodeDTO = await _userService.GetMyNodeAsync(userId);
                if (nodeDTO == null)
                {
                    return NotFound(new { error = "Personal node not found" });
                }

                return Ok(nodeDTO);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpPut("my-node")]
        public async Task<IActionResult> UpdateMyNode([FromBody] UpdateMyNodeDTO updateDto)
        {
            var userIdClaim = User.FindFirst("Id")?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            try
            {
                var nodeDTO = await _userService.UpdateMyNodeAsync(userId, updateDto);
                return Ok(new { message = "Personal node updated successfully", node = nodeDTO });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsers()
        {
            var userDTOs = await _userService.GetAllUsersAsync();
            return Ok(userDTOs);
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers([FromQuery] string? query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(new { error = "Search query cannot be empty" });
            }

            var userIdClaim = User.FindFirst("Id")?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int currentUserId))
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            try
            {
                var userDTOs = await _userService.SearchUsersAsync(query, currentUserId);
                return Ok(userDTOs);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Update user profile (username, phone, password, and profile photo)
        /// </summary>
        [HttpPut("profile")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateProfile(
            [FromForm] string? userName,
            [FromForm] string? phoneNumber,
            [FromForm] string? password,
            IFormFile? profilePhoto)
        {
            var userIdClaim = User.FindFirst("Id")?.Value;
            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { error = "Invalid token" });
            }

            try
            {
                var updateDto = new UpdateUserProfileDTO
                {
                    UserName = userName,
                    PhoneNumber = phoneNumber,
                    Password = password
                };

                var updatedProfile = await _userService.UpdateProfileWithPhotoAsync(userId, updateDto, profilePhoto?.ToFileUploadDTO(), _environment.ContentRootPath);

                return Ok(new { message = "Profile updated successfully", profile = updatedProfile });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating profile", details = ex.Message });
            }
        }
    }
}