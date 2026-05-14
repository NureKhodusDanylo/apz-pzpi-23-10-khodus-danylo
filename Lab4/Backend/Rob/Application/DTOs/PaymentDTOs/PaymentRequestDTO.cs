namespace Application.DTOs.PaymentDTOs;

public class PaymentRequestDTO
{
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty; // PayPal, GooglePay, Stripe
    public string Currency { get; set; } = "UAH";

    // Optional fields for specific payment methods
    public string? PayPalEmail { get; set; }
    public string? GooglePayToken { get; set; }
    public string? StripeCardToken { get; set; }
}
