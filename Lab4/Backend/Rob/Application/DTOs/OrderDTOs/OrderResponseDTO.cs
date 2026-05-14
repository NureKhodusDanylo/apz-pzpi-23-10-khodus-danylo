using Application.DTOs.FileDTOs;
using Entities.Models;

namespace Application.DTOs.OrderDTOs
{
    public class OrderResponseDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public double Weight { get; set; }
        public decimal DeliveryPrice { get; set; }
        public decimal ProductPrice { get; set; }
        public bool IsProductPaid { get; set; }

        // Delivery payment fields
        public DeliveryPayer DeliveryPayer { get; set; }
        public string DeliveryPayerName { get; set; }  // "Sender" or "Recipient" for display
        public bool IsDeliveryPaid { get; set; }

        public OrderStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public int SenderId { get; set; }
        public string SenderName { get; set; }

        public int RecipientId { get; set; }
        public string RecipientName { get; set; }

        public int? RobotId { get; set; }
        public string? RobotName { get; set; }

        public int PickupNodeId { get; set; }
        public string PickupNodeName { get; set; }

        public int DropoffNodeId { get; set; }
        public string DropoffNodeName { get; set; }

        // Collection of order images
        public List<FileResponseDTO>? Images { get; set; }
    }
}
