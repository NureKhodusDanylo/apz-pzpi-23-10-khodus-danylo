namespace Application.DTOs.PaymentDTOs;

public class PaymentResultDTO
{
    public bool Success { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "UAH";
    public DateTime ProcessedAt { get; set; }
    public string? ErrorMessage { get; set; }

    // Order payment details
    public int? OrderId { get; set; }
    public bool ProductPaid { get; set; }
    public bool DeliveryPaid { get; set; }
}
