using Application.Abstractions.Interfaces;
using Application.DTOs.AdminDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RobDeliveryAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly ISettingsService _settingsService;

        public AdminController(IAdminService adminService, ISettingsService settingsService)
        {
            _adminService = adminService;
            _settingsService = settingsService;
        }

        /// <summary>
        /// Get system statistics for dashboard
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetSystemStats()
        {
            try
            {
                var stats = await _adminService.GetSystemStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve system stats", details = ex.Message });
            }
        }

        /// <summary>
        /// Export delivery history as JSON
        /// </summary>
        [HttpGet("export/delivery-history")]
        public async Task<IActionResult> ExportDeliveryHistory()
        {
            try
            {
                var historyJson = await _adminService.ExportDeliveryHistoryAsync();
                var fileName = $"DeliveryHistory_{DateTime.Now:yyyyMMdd_HHmmss}.json";

                return File(
                    System.Text.Encoding.UTF8.GetBytes(historyJson),
                    "application/json",
                    fileName
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to export delivery history", details = ex.Message });
            }
        }

        /// <summary>
        /// Create database backup
        /// </summary>
        [HttpPost("backup")]
        public async Task<IActionResult> CreateBackup([FromBody] BackupRequestDTO request)
        {
            try
            {
                string backupPath = request.BackupPath ?? "Backups";
                var success = await _adminService.CreateDatabaseBackupAsync(backupPath);

                if (success)
                {
                    return Ok(new { message = "Backup created successfully", path = backupPath });
                }
                else
                {
                    return StatusCode(500, new { error = "Failed to create backup" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to create backup", details = ex.Message });
            }
        }

        /// <summary>
        /// Get robot efficiency analytics
        /// </summary>
        [HttpGet("analytics/robot-efficiency")]
        public async Task<IActionResult> GetRobotEfficiency()
        {
            try
            {
                var efficiency = await _adminService.GetRobotEfficiencyAsync();
                return Ok(efficiency);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve robot efficiency", details = ex.Message });
            }
        }

        /// <summary>
        /// Generate a new admin registration key
        /// </summary>
        [HttpPost("keys/generate")]
        public async Task<IActionResult> GenerateAdminKey([FromBody] CreateAdminKeyDTO request)
        {
            try
            {
                var userIdClaim = User.FindFirst("Id")?.Value;
                if (userIdClaim == null || !int.TryParse(userIdClaim, out int adminId))
                {
                    return Unauthorized(new { error = "Invalid token" });
                }

                var adminKey = await _adminService.GenerateAdminKeyAsync(
                    adminId,
                    request.ExpiresAt,
                    request.Description
                );

                return Ok(new
                {
                    message = "Admin key generated successfully",
                    key = adminKey
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to generate admin key", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all admin keys
        /// </summary>
        [HttpGet("keys")]
        public async Task<IActionResult> GetAllAdminKeys()
        {
            try
            {
                var keys = await _adminService.GetAllAdminKeysAsync();
                return Ok(keys);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve admin keys", details = ex.Message });
            }
        }

        /// <summary>
        /// Get unused admin keys
        /// </summary>
        [HttpGet("keys/unused")]
        public async Task<IActionResult> GetUnusedAdminKeys()
        {
            try
            {
                var keys = await _adminService.GetUnusedAdminKeysAsync();
                return Ok(keys);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve unused admin keys", details = ex.Message });
            }
        }

        /// <summary>
        /// Revoke an admin key
        /// </summary>
        [HttpPost("keys/{keyId}/revoke")]
        public async Task<IActionResult> RevokeAdminKey(int keyId)
        {
            try
            {
                var success = await _adminService.RevokeAdminKeyAsync(keyId);

                if (success)
                {
                    return Ok(new { message = "Admin key revoked successfully" });
                }
                else
                {
                    return NotFound(new { error = "Admin key not found or already used" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to revoke admin key", details = ex.Message });
            }
        }

        /// <summary>
        /// Get current delivery pricing settings
        /// </summary>
        [HttpGet("pricing")]
        public async Task<IActionResult> GetPricingSettings()
        {
            try
            {
                var settings = await _settingsService.GetPricingSettingsAsync();
                return Ok(settings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve pricing settings", details = ex.Message });
            }
        }

        /// <summary>
        /// Update delivery pricing settings
        /// </summary>
        [HttpPut("pricing")]
        public async Task<IActionResult> UpdatePricingSettings([FromBody] UpdatePricingDTO request)
        {
            try
            {
                var success = await _settingsService.UpdatePricingSettingsAsync(request);
                if (success)
                {
                    return Ok(new { message = "Pricing settings updated successfully" });
                }
                return BadRequest(new { error = "Failed to update pricing settings" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to update pricing settings", details = ex.Message });
            }
        }
    }

    public class BackupRequestDTO
    {
        public string? BackupPath { get; set; }
    }
}
