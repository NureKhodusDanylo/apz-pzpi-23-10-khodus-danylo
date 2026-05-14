namespace Application.DTOs.RobotDTOs
{
    /// <summary>
    /// Response DTO when robot accepts an order
    /// </summary>
    public class AcceptOrderResponseDTO
    {
        public string Message { get; set; } = string.Empty;
        public int OrderId { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public DateTime AcceptedAt { get; set; }
    }
}
