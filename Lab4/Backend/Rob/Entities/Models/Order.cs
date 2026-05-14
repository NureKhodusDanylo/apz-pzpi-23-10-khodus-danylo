using Entities.Interfaces;

namespace Entities.Models
{
    public class Order : IDbEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public double Weight { get; set; }

        // Pricing fields
        public decimal DeliveryPrice { get; set; }  // Price for delivery service
        public decimal ProductPrice { get; set; }   // Price of the product (for insurance/compensation)
        public bool IsProductPaid { get; set; }     // Whether user has paid for the product

        // Delivery payment fields
        public DeliveryPayer DeliveryPayer { get; set; } = DeliveryPayer.Sender;  // Who pays for delivery
        public bool IsDeliveryPaid { get; set; } = false;  // Whether delivery has been paid

        public OrderStatus Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }


        public int SenderId { get; set; }
        public virtual User Sender { get; set; }

        public int RecipientId { get; set; }
        public virtual User Recipient { get; set; }

        public int? RobotId { get; set; }
        public virtual Robot AssignedRobot { get; set; }

        public int PickupNodeId { get; set; }
        public virtual Node PickupNode { get; set; }

        public int DropoffNodeId { get; set; }
        public virtual Node DropoffNode { get; set; }

        // Collection of images for this order
        public virtual ICollection<File> Images { get; set; }
    }
}