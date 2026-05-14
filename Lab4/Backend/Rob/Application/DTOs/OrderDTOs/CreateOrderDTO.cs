using Entities.Models;

namespace Application.DTOs.OrderDTOs
{
    public class CreateOrderDTO
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public double Weight { get; set; }
        public decimal ProductPrice { get; set; }
        public bool IsProductPaid { get; set; }
        public int RecipientId { get; set; }

        /// <summary>
        /// Who should pay for delivery: Sender (0) or Recipient (1). Default: Sender
        /// </summary>
        public DeliveryPayer DeliveryPayer { get; set; } = DeliveryPayer.Sender;
    }
}
