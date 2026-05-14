using Entities.Models;

namespace Entities.Interfaces
{
    public interface IOrderRepository
    {
        Task AddAsync(Order order);
        Task<Order?> GetByIdAsync(int orderId);
        Task<IEnumerable<Order>> GetAllAsync();
        Task<IEnumerable<Order>> GetBySenderIdAsync(int senderId);
        Task<IEnumerable<Order>> GetByRecipientIdAsync(int recipientId);
        Task<IEnumerable<Order>> GetByUserIdAsync(int userId);
        Task<IEnumerable<Order>> GetByStatusAsync(OrderStatus status);
        Task UpdateAsync(Order order);
        Task DeleteAsync(int orderId);
        Task<bool> ExistsAsync(int orderId);
        Task<bool> DoesItBelong(int orderId, int userId);
        Task SaveChangesAsync();
    }
}
