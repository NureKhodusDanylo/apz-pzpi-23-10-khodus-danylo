using Application.DTOs.OrderDTOs;

namespace Application.Abstractions.Interfaces
{
    public interface IOrderService
    {
        Task<OrderResponseDTO> CreateOrderAsync(int senderId, CreateOrderDTO orderDto);
        Task<OrderResponseDTO?> GetOrderByIdAsync(int orderId);
        Task<IEnumerable<OrderResponseDTO>> GetAllOrdersAsync();
        Task<IEnumerable<OrderResponseDTO>> GetUserOrdersAsync(int userId);
        Task<IEnumerable<OrderResponseDTO>> GetSentOrdersAsync(int senderId);
        Task<IEnumerable<OrderResponseDTO>> GetReceivedOrdersAsync(int recipientId);
        Task<IEnumerable<OrderResponseDTO>> GetOrdersByStatusAsync(OrderStatus status);
        Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus);
        Task<bool> AssignRobotToOrderAsync(int orderId, int robotId);
        Task<bool> CancelOrderAsync(int orderId, int userId);
        Task<bool> DeleteOrderAsync(int orderId);
        Task<ExecuteOrderResponseDTO> ExecuteOrderAsync(int orderId, int userId);
        Task<decimal> CalculateDeliveryPriceAsync(double weight);
    }
}
