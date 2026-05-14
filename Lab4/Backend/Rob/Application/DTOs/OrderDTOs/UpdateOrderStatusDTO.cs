namespace Application.DTOs.OrderDTOs
{
    public class UpdateOrderStatusDTO
    {
        public int OrderId { get; set; }
        public OrderStatus NewStatus { get; set; }
    }
}
