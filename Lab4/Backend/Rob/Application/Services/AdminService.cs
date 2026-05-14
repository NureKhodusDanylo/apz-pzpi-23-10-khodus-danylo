using Application.Abstractions.Interfaces;
using Application.DTOs.AdminDTOs;
using Entities.Interfaces;
using Entities.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Application.Services
{
    public class AdminService : IAdminService
    {
        private readonly IUserRepository _userRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IRobotRepository _robotRepository;
        private readonly INodeRepository _nodeRepository;
        private readonly IAdminKeyRepository _adminKeyRepository;

        public AdminService(
            IUserRepository userRepository,
            IOrderRepository orderRepository,
            IRobotRepository robotRepository,
            INodeRepository nodeRepository,
            IAdminKeyRepository adminKeyRepository)
        {
            _userRepository = userRepository;
            _orderRepository = orderRepository;
            _robotRepository = robotRepository;
            _nodeRepository = nodeRepository;
            _adminKeyRepository = adminKeyRepository;
        }

        public async Task<SystemStatsDTO> GetSystemStatsAsync()
        {
            var users = await _userRepository.GetAllAsync();
            var orders = await _orderRepository.GetAllAsync();
            var robots = await _robotRepository.GetAllAsync();
            var nodes = await _nodeRepository.GetAllAsync();

            var ordersList = orders.ToList();
            var robotsList = robots.ToList();

            var stats = new SystemStatsDTO
            {
                TotalUsers = users.Count(),
                TotalOrders = ordersList.Count,
                TotalRobots = robotsList.Count,
                TotalNodes = nodes.Count(),

                ActiveOrders = ordersList.Count(o => o.Status == OrderStatus.Processing || o.Status == OrderStatus.EnRoute),
                CompletedOrders = ordersList.Count(o => o.Status == OrderStatus.Delivered),
                CancelledOrders = ordersList.Count(o => o.Status == OrderStatus.Cancelled),

                AvailableRobots = robotsList.Count(r => r.Status == RobotStatus.Idle),
                BusyRobots = robotsList.Count(r => r.Status == RobotStatus.Delivering),
                ChargingRobots = robotsList.Count(r => r.Status == RobotStatus.Charging),

                AverageBatteryLevel = robotsList.Any() ? robotsList.Average(r => r.BatteryLevel) : 0,
                TotalRevenue = (double)ordersList.Where(o => o.IsProductPaid && o.Status == OrderStatus.Delivered).Sum(o => o.DeliveryPrice + o.ProductPrice)
            };

            return stats;
        }

        public async Task<string> ExportDeliveryHistoryAsync()
        {
            var orders = await _orderRepository.GetAllAsync();

            var deliveryHistory = orders.Select(o => new
            {
                o.Id,
                o.Name,
                o.Description,
                o.Weight,
                o.DeliveryPrice,
                o.ProductPrice,
                o.IsProductPaid,
                Status = o.Status.ToString(),
                o.CreatedAt,
                o.CompletedAt,
                SenderId = o.SenderId,
                SenderName = o.Sender?.UserName,
                RecipientId = o.RecipientId,
                RecipientName = o.Recipient?.UserName,
                RobotId = o.RobotId,
                RobotName = o.AssignedRobot?.Name,
                PickupNodeId = o.PickupNodeId,
                PickupNodeName = o.PickupNode?.Name,
                DropoffNodeId = o.DropoffNodeId,
                DropoffNodeName = o.DropoffNode?.Name
            }).ToList();

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return JsonSerializer.Serialize(deliveryHistory, options);
        }

        public async Task<bool> CreateDatabaseBackupAsync(string backupPath)
        {
            try
            {
                // For SQLite, create a backup by copying the database file
                // In production, use proper backup strategies

                // Try multiple possible paths
                string[] possiblePaths = new[]
                {
                    Path.Combine(Directory.GetCurrentDirectory(), "Infrastructure", "DB_Storage", "RobDelivery.db"),
                    Path.Combine(Directory.GetCurrentDirectory(), "..", "Infrastructure", "DB_Storage", "RobDelivery.db"),
                    Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Infrastructure", "DB_Storage", "RobDelivery.db")
                };

                string? sourceDbPath = null;
                foreach (var path in possiblePaths)
                {
                    var fullPath = Path.GetFullPath(path);
                    if (System.IO.File.Exists(fullPath))
                    {
                        sourceDbPath = fullPath;
                        break;
                    }
                }

                if (sourceDbPath == null || !System.IO.File.Exists(sourceDbPath))
                {
                    throw new FileNotFoundException($"Database file not found. Tried paths: {string.Join(", ", possiblePaths.Select(Path.GetFullPath))}");
                }

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupFileName = $"RobDelivery_Backup_{timestamp}.db";
                string fullBackupPath = Path.Combine(backupPath, backupFileName);

                // Ensure backup directory exists
                Directory.CreateDirectory(backupPath);

                // Copy database file
                System.IO.File.Copy(sourceDbPath, fullBackupPath, overwrite: true);

                // Also export delivery history as JSON
                string historyJson = await ExportDeliveryHistoryAsync();
                string jsonBackupPath = Path.Combine(backupPath, $"DeliveryHistory_{timestamp}.json");
                await System.IO.File.WriteAllTextAsync(jsonBackupPath, historyJson);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<Dictionary<int, double>> GetRobotEfficiencyAsync()
        {
            var robots = await _robotRepository.GetAllAsync();
            var efficiency = new Dictionary<int, double>();

            foreach (var robot in robots)
            {
                // Calculate efficiency based on completed deliveries and battery usage
                // Efficiency = (Number of completed orders) / (100 - Average battery level)
                // This is a simplified metric - in production, use more sophisticated analytics

                int completedOrders = robot.ActiveOrders?.Count(o => o.Status == OrderStatus.Delivered) ?? 0;
                double batteryEfficiency = robot.BatteryLevel > 0 ? completedOrders / (101 - robot.BatteryLevel) : 0;

                efficiency[robot.Id] = Math.Round(batteryEfficiency * 100, 2);
            }

            return efficiency;
        }

        public async Task<AdminKeyDTO> GenerateAdminKeyAsync(int createdByAdminId, DateTime? expiresAt = null, string? description = null)
        {
            // Generate a unique secure key code
            string keyCode = GenerateSecureKeyCode();

            var adminKey = new AdminKey
            {
                KeyCode = keyCode,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt,
                IsUsed = false,
                CreatedByAdminId = createdByAdminId,
                Description = description
            };

            await _adminKeyRepository.AddAsync(adminKey);
            await _adminKeyRepository.SaveChangesAsync();

            // Reload to get navigation properties
            var createdKey = await _adminKeyRepository.GetByKeyCodeAsync(keyCode);

            return MapToDTO(createdKey!);
        }

        public async Task<IEnumerable<AdminKeyDTO>> GetAllAdminKeysAsync()
        {
            var keys = await _adminKeyRepository.GetAllAsync();
            return keys.Select(MapToDTO);
        }

        public async Task<IEnumerable<AdminKeyDTO>> GetUnusedAdminKeysAsync()
        {
            var keys = await _adminKeyRepository.GetUnusedKeysAsync();
            return keys.Select(MapToDTO);
        }

        public async Task<bool> RevokeAdminKeyAsync(int keyId)
        {
            var key = await _adminKeyRepository.GetByIdAsync(keyId);
            if (key == null || key.IsUsed)
                return false;

            key.IsUsed = true;
            key.UsedAt = DateTime.UtcNow;

            await _adminKeyRepository.UpdateAsync(key);
            await _adminKeyRepository.SaveChangesAsync();

            return true;
        }

        private string GenerateSecureKeyCode()
        {
            // Generate a secure random key using cryptographic random
            const int keyLength = 32;
            var randomBytes = new byte[keyLength];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            // Convert to base64 and make it URL-safe
            string keyCode = Convert.ToBase64String(randomBytes)
                .Replace("+", "")
                .Replace("/", "")
                .Replace("=", "")
                .Substring(0, 24); // Take first 24 characters for readability

            return $"ADMIN-{keyCode}";
        }

        private AdminKeyDTO MapToDTO(AdminKey key)
        {
            return new AdminKeyDTO
            {
                Id = key.Id,
                KeyCode = key.KeyCode,
                CreatedAt = key.CreatedAt,
                ExpiresAt = key.ExpiresAt,
                IsUsed = key.IsUsed,
                UsedAt = key.UsedAt,
                UsedByUserId = key.UsedByUserId,
                UsedByUserName = key.UsedByUser?.UserName,
                CreatedByAdminId = key.CreatedByAdminId,
                CreatedByAdminName = key.CreatedByAdmin?.UserName ?? "Unknown",
                Description = key.Description
            };
        }
    }
}
