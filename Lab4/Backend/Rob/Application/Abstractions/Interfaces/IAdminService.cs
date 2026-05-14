using Application.DTOs.AdminDTOs;

namespace Application.Abstractions.Interfaces
{
    public interface IAdminService
    {
        /// <summary>
        /// Get system statistics for admin dashboard
        /// </summary>
        Task<SystemStatsDTO> GetSystemStatsAsync();

        /// <summary>
        /// Export all delivery history to JSON format
        /// </summary>
        Task<string> ExportDeliveryHistoryAsync();

        /// <summary>
        /// Create database backup
        /// </summary>
        Task<bool> CreateDatabaseBackupAsync(string backupPath);

        /// <summary>
        /// Get robot efficiency analytics
        /// </summary>
        Task<Dictionary<int, double>> GetRobotEfficiencyAsync();

        /// <summary>
        /// Generate a new admin registration key
        /// </summary>
        Task<AdminKeyDTO> GenerateAdminKeyAsync(int createdByAdminId, DateTime? expiresAt = null, string? description = null);

        /// <summary>
        /// Get all admin keys
        /// </summary>
        Task<IEnumerable<AdminKeyDTO>> GetAllAdminKeysAsync();

        /// <summary>
        /// Get unused admin keys
        /// </summary>
        Task<IEnumerable<AdminKeyDTO>> GetUnusedAdminKeysAsync();

        /// <summary>
        /// Revoke an admin key (mark as used)
        /// </summary>
        Task<bool> RevokeAdminKeyAsync(int keyId);
    }
}
