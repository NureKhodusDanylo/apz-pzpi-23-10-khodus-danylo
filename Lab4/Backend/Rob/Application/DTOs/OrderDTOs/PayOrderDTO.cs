namespace Application.DTOs.OrderDTOs
{
    /// <summary>
    /// DTO for paying order product and/or delivery
    /// </summary>
    public class PayOrderDTO
    {
        public int OrderId { get; set; }

        /// <summary>
        /// Pay for the product (will set IsProductPaid = true)
        /// </summary>
        public bool PayProduct { get; set; }

        /// <summary>
        /// Pay for the delivery (will set IsDeliveryPaid = true)
        /// </summary>
        public bool PayDelivery { get; set; }

        /// <summary>
        /// Payment method: PayPal, GooglePay, or Stripe
        /// </summary>
        public string PaymentMethod { get; set; } = string.Empty;

        /// <summary>
        /// Optional token for Stripe payments
        /// </summary>
        public string? StripeCardToken { get; set; }
    }
}
