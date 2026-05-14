using Application.Abstractions.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RobDeliveryAPI.Extensions;
using System.Security.Claims;

namespace RobDeliveryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FileController : ControllerBase
    {
        private readonly IFileService _fileService;
        private readonly IWebHostEnvironment _environment;

        public FileController(
            IFileService fileService,
            IWebHostEnvironment environment)
        {
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
        /// Upload images for an order
        /// </summary>
        [HttpPost("order/{orderId}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadOrderImages(int orderId, IFormFileCollection files)
        {
            try
            {
                int userId = GetAuthenticatedUserId();

                var uploadedFiles = await _fileService.UploadOrderImagesAsync(orderId, files.ToFileUploadDTOs(), _environment.ContentRootPath);

                return Ok(new { message = $"{uploadedFiles.Count} file(s) uploaded successfully", files = uploadedFiles });
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
                return StatusCode(500, new { error = "An error occurred while uploading files", details = ex.Message });
            }
        }

        /// <summary>
        /// Delete a file
        /// </summary>
        [HttpDelete("{fileId}")]
        public async Task<IActionResult> DeleteFile(int fileId)
        {
            try
            {
                int userId = GetAuthenticatedUserId();
                bool isAdmin = User.IsInRole("Admin");

                // Check authorization
                bool isAuthorized = await _fileService.IsUserAuthorizedForFileAsync(fileId, userId, isAdmin);
                if (!isAuthorized)
                {
                    return StatusCode(403, new { error = "You do not have permission to delete this file" });
                }

                // Delete file
                var result = await _fileService.DeleteFileAsync(fileId, _environment.ContentRootPath);
                if (!result)
                {
                    return NotFound(new { error = "File not found" });
                }

                return Ok(new { message = "File deleted successfully" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while deleting the file", details = ex.Message });
            }
        }

        /// <summary>
        /// Get files for an order
        /// </summary>
        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetOrderFiles(int orderId)
        {
            try
            {
                var files = await _fileService.GetOrderFilesAsync(orderId);
                return Ok(files);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving files", details = ex.Message });
            }
        }

        /// <summary>
        /// Download a file by ID
        /// </summary>
        [HttpGet("{fileId}/download")]
        [AllowAnonymous]
        public async Task<IActionResult> DownloadFile(int fileId)
        {
            try
            {
                var file = await _fileService.GetFileByIdAsync(fileId);
                if (file == null)
                {
                    return NotFound(new { error = "File not found" });
                }

                var fileBytes = await _fileService.GetFileContentAsync(fileId, _environment.ContentRootPath);
                if (fileBytes == null)
                {
                    return NotFound(new { error = "File not found on disk" });
                }

                return File(fileBytes, file.ContentType, file.FileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while downloading the file", details = ex.Message });
            }
        }
    }
}
