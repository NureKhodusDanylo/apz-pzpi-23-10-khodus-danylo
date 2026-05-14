using Application.DTOs.PaymentDTOs;

namespace Application.Abstractions.Interfaces;

public interface IPaymentService
{
    Task<PaymentResultDTO> ProcessPaymentAsync(PaymentRequestDTO request);
    Task<bool> RefundPaymentAsync(string transactionId, decimal amount);
    Task<bool> ValidatePaymentDetailsAsync(PaymentRequestDTO request);
}
