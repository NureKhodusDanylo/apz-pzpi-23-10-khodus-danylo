using Entities.Interfaces;
using Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository
{
    public class RobotRepository : GenericRepository<Robot>, IRobotRepository
    {
        private readonly MyDbContext _context;

        public RobotRepository(MyDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Robot?> GetByIdAsync(int robotId)
        {
            return await _context.Robots
                .Include(r => r.CurrentNode)
                .Include(r => r.ActiveOrders)
                .FirstOrDefaultAsync(r => r.Id == robotId);
        }

        public async Task<IEnumerable<Robot>> GetAllAsync()
        {
            return await _context.Robots
                .Include(r => r.CurrentNode)
                .Include(r => r.ActiveOrders)
                .OrderBy(r => r.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Robot>> GetByStatusAsync(RobotStatus status)
        {
            return await _context.Robots
                .Include(r => r.CurrentNode)
                .Include(r => r.ActiveOrders)
                .Where(r => r.Status == status)
                .OrderBy(r => r.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Robot>> GetByTypeAsync(RobotType type)
        {
            return await _context.Robots
                .Include(r => r.CurrentNode)
                .Include(r => r.ActiveOrders)
                .Where(r => r.Type == type)
                .OrderBy(r => r.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Robot>> GetByTypeAndStatusAsync(RobotType type, RobotStatus status)
        {
            return await _context.Robots
                .Include(r => r.CurrentNode)
                .Include(r => r.ActiveOrders)
                .Where(r => r.Type == type && r.Status == status)
                .OrderByDescending(r => r.BatteryLevel)
                .ToListAsync();
        }

        public async Task<IEnumerable<Robot>> GetAvailableRobotsAsync()
        {
            return await _context.Robots
                .Include(r => r.CurrentNode)
                .Include(r => r.ActiveOrders)
                .Where(r =>
                    (r.Status == RobotStatus.Idle && r.BatteryLevel >= 20) ||
                    (r.Status == RobotStatus.Charging && r.BatteryLevel >= 20))
                .OrderByDescending(r => r.BatteryLevel)
                .ToListAsync();
        }

        public async Task UpdateAsync(Robot robot)
        {
            _context.Robots.Update(robot);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int robotId)
        {
            var robot = await _context.Robots.FindAsync(robotId);
            if (robot != null)
            {
                _context.Robots.Remove(robot);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int robotId)
        {
            return await _context.Robots.AnyAsync(r => r.Id == robotId);
        }

        public async Task<Robot?> GetBySerialNumberAsync(string serialNumber)
        {
            return await _context.Robots
                .Include(r => r.CurrentNode)
                .Include(r => r.TargetNode)
                .Include(r => r.ActiveOrders)
                .FirstOrDefaultAsync(r => r.SerialNumber == serialNumber);
        }

        public async Task<bool> SerialNumberExistsAsync(string serialNumber)
        {
            return await _context.Robots.AnyAsync(r => r.SerialNumber == serialNumber);
        }
    }
}
