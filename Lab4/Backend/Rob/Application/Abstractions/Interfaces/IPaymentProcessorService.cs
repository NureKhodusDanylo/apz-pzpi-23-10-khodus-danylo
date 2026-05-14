using Application.DTOs.PaymentDTOs;

namespace Application.Abstractions.Interfaces
{
    public interface IPaymentProcessorService
    {
        Task<PaymentResultDTO> ProcessPaymentAsync(PaymentRequestDTO request);
        Task<bool> RefundPaymentAsync(string paymentMethod, string transactionId, decimal amount);
    }
}
