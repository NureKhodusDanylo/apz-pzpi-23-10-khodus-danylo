using Entities.Interfaces;
using Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository
{
    public class OrderRepository : GenericRepository<Order>, IOrderRepository
    {
        private readonly MyDbContext _context;

        public OrderRepository(MyDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Order?> GetByIdAsync(int orderId)
        {
            return await _context.Orders
                .Include(o => o.Sender)
                .Include(o => o.Recipient)
                .Include(o => o.AssignedRobot)
                .Include(o => o.PickupNode)
                .Include(o => o.DropoffNode)
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        public async Task<IEnumerable<Order>> GetAllAsync()
        {
            return await _context.Orders
                .Include(o => o.Sender)
                .Include(o => o.Recipient)
                .Include(o => o.AssignedRobot)
                .Include(o => o.PickupNode)
                .Include(o => o.DropoffNode)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Order>> GetBySenderIdAsync(int senderId)
        {
            return await _context.Orders
                .Include(o => o.Sender)
                .Include(o => o.Recipient)
                .Include(o => o.AssignedRobot)
                .Include(o => o.PickupNode)
                .Include(o => o.DropoffNode)
                .Where(o => o.SenderId == senderId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Order>> GetByRecipientIdAsync(int recipientId)
        {
            return await _context.Orders
                .Include(o => o.Sender)
                .Include(o => o.Recipient)
                .Include(o => o.AssignedRobot)
                .Include(o => o.PickupNode)
                .Include(o => o.DropoffNode)
                .Where(o => o.RecipientId == recipientId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Order>> GetByUserIdAsync(int userId)
        {
            return await _context.Orders
                .Include(o => o.Sender)
                .Include(o => o.Recipient)
                .Include(o => o.AssignedRobot)
                .Include(o => o.PickupNode)
                .Include(o => o.DropoffNode)
                .Where(o => o.SenderId == userId || o.RecipientId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Order>> GetByStatusAsync(OrderStatus status)
        {
            return await _context.Orders
                .Include(o => o.Sender)
                .Include(o => o.Recipient)
                .Include(o => o.AssignedRobot)
                .Include(o => o.PickupNode)
                .Include(o => o.DropoffNode)
                .Where(o => o.Status == status)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task UpdateAsync(Order order)
        {
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int orderId)
        {
            return await _context.Orders.AnyAsync(o => o.Id == orderId);
        }

        public async Task<bool> DoesItBelong(int orderId, int userId)
        {
            return await _context.Orders
                .AnyAsync(o => o.Id == orderId && o.SenderId == userId);
        }

    }
}
