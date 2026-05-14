using Entities.Models;

namespace Entities.Interfaces
{
    public interface IRobotRepository
    {
        Task AddAsync(Robot robot);
        Task<Robot?> GetByIdAsync(int robotId);
        Task<Robot?> GetBySerialNumberAsync(string serialNumber);
        Task<IEnumerable<Robot>> GetAllAsync();
        Task<IEnumerable<Robot>> GetByStatusAsync(RobotStatus status);
        Task<IEnumerable<Robot>> GetByTypeAsync(RobotType type);
        Task<IEnumerable<Robot>> GetByTypeAndStatusAsync(RobotType type, RobotStatus status);
        Task<IEnumerable<Robot>> GetAvailableRobotsAsync();
        Task UpdateAsync(Robot robot);
        Task DeleteAsync(int robotId);
        Task<bool> ExistsAsync(int robotId);
        Task<bool> SerialNumberExistsAsync(string serialNumber);
        Task SaveChangesAsync();
    }
}
