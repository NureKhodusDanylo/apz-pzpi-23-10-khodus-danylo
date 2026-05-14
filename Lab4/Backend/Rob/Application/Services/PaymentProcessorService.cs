using Application.Abstractions.Interfaces;
using Application.DTOs.PaymentDTOs;
using Application.Services.PaymentServices;

namespace Application.Services;

public class PaymentProcessorService : IPaymentProcessorService
{
    private readonly IPaymentService _payPalService;
    private readonly IPaymentService _googlePayService;
    private readonly IPaymentService _stripeService;

    public PaymentProcessorService(
        PayPalPaymentService payPalService,
        GooglePayPaymentService googlePayService,
        StripePaymentService stripeService)
    {
        _payPalService = payPalService;
        _googlePayService = googlePayService;
        _stripeService = stripeService;
    }

    public async Task<PaymentResultDTO> ProcessPaymentAsync(PaymentRequestDTO request)
    {
        // Select the appropriate payment service based on payment method
        IPaymentService paymentService = request.PaymentMethod.ToLower() switch
        {
            "paypal" => _payPalService,
            "googlepay" => _googlePayService,
            "stripe" => _stripeService,
            _ => throw new ArgumentException($"Unsupported payment method: {request.PaymentMethod}")
        };

        // Validate payment details before processing
        var isValid = await paymentService.ValidatePaymentDetailsAsync(request);
        if (!isValid)
        {
            return new PaymentResultDTO
            {
                Success = false,
                TransactionId = string.Empty,
                PaymentMethod = request.PaymentMethod,
                Amount = request.Amount,
                Currency = request.Currency,
                ProcessedAt = DateTime.UtcNow,
                ErrorMessage = "Payment details validation failed"
            };
        }

        // Process the payment
        return await paymentService.ProcessPaymentAsync(request);
    }

    public async Task<bool> RefundPaymentAsync(string paymentMethod, string transactionId, decimal amount)
    {
        IPaymentService paymentService = paymentMethod.ToLower() switch
        {
            "paypal" => _payPalService,
            "googlepay" => _googlePayService,
            "stripe" => _stripeService,
            _ => throw new ArgumentException($"Unsupported payment method: {paymentMethod}")
        };

        return await paymentService.RefundPaymentAsync(transactionId, amount);
    }
}
