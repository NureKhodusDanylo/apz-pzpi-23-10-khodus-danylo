using Application.Abstractions.Interfaces;
using Application.DTOs.PaymentDTOs;
using Entities.Config;
using Microsoft.Extensions.Logging;
using Stripe;

namespace Application.Services.PaymentServices;

public class StripePaymentService : IPaymentService
{
    private readonly ILogger<StripePaymentService> _logger;
    private readonly StripeSettings _settings;

    public StripePaymentService(Config config, ILogger<StripePaymentService> logger)
    {
        _settings = config.Payment.Stripe;
        _logger = logger;

        // Set Stripe API key globally
        StripeConfiguration.ApiKey = _settings.SecretKey;
    }

    public async Task<PaymentResultDTO> ProcessPaymentAsync(PaymentRequestDTO request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.StripeCardToken))
            {
                return CreateErrorResult(request, "Stripe card token is required");
            }

            // Validate token format
            if (!request.StripeCardToken.StartsWith("tok_") &&
                !request.StripeCardToken.StartsWith("pm_") &&
                !request.StripeCardToken.StartsWith("card_"))
            {
                return CreateErrorResult(request, "Invalid Stripe token format");
            }

            // Convert UAH amount to smallest currency unit (kopiykas/cents)
            var amountInCents = (long)(request.Amount * 100);

            // Create PaymentIntent
            var paymentIntentService = new PaymentIntentService();
            var paymentIntentOptions = new PaymentIntentCreateOptions
            {
                Amount = amountInCents,
                Currency = request.Currency.ToLower(),
                PaymentMethod = request.StripeCardToken,
                Confirm = true, // Auto-confirm payment
                Description = $"RobDelivery Order #{request.OrderId}",
                Metadata = new Dictionary<string, string>
                {
                    { "order_id", request.OrderId.ToString() },
                    { "service", "RobDelivery" }
                },
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                    AllowRedirects = "never"
                }
            };

            var paymentIntent = await paymentIntentService.CreateAsync(paymentIntentOptions);

            _logger.LogInformation($"Stripe PaymentIntent created: {paymentIntent.Id}, Status: {paymentIntent.Status}");

            // Check payment status
            if (paymentIntent.Status == "succeeded")
            {
                return new PaymentResultDTO
                {
                    Success = true,
                    TransactionId = paymentIntent.Id,
                    PaymentMethod = "Stripe",
                    Amount = request.Amount,
                    Currency = request.Currency,
                    ProcessedAt = DateTime.UtcNow,
                    ErrorMessage = null
                };
            }
            else if (paymentIntent.Status == "requires_action" || paymentIntent.Status == "requires_source_action")
            {
                return CreateErrorResult(request, "Payment requires additional authentication (3D Secure)");
            }
            else
            {
                return CreateErrorResult(request, $"Payment failed with status: {paymentIntent.Status}");
            }
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, $"Stripe payment failed: {ex.StripeError.Message}");
            return CreateErrorResult(request, $"Stripe error: {ex.StripeError.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Stripe payment");
            return CreateErrorResult(request, $"Payment failed: {ex.Message}");
        }
    }

    public async Task<bool> RefundPaymentAsync(string transactionId, decimal amount)
    {
        try
        {
            var refundService = new RefundService();
            var refundOptions = new RefundCreateOptions
            {
                PaymentIntent = transactionId,
                Amount = (long)(amount * 100), // Convert to cents
                Reason = RefundReasons.RequestedByCustomer
            };

            var refund = await refundService.CreateAsync(refundOptions);

            _logger.LogInformation($"Stripe refund completed: {refund.Id}, Status: {refund.Status}");

            return refund.Status == "succeeded" || refund.Status == "pending";
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, $"Stripe refund failed for transaction {transactionId}: {ex.StripeError.Message}");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Unexpected error during Stripe refund for transaction {transactionId}");
            return false;
        }
    }

    public async Task<bool> ValidatePaymentDetailsAsync(PaymentRequestDTO request)
    {
        await Task.CompletedTask;

        if (string.IsNullOrEmpty(request.StripeCardToken))
            return false;

        // Validate token format
        if (!request.StripeCardToken.StartsWith("tok_") &&
            !request.StripeCardToken.StartsWith("pm_") &&
            !request.StripeCardToken.StartsWith("card_"))
            return false;

        if (request.Amount <= 0)
            return false;

        // Minimum amount validation (50 cents for most currencies)
        if (request.Amount < 0.50m)
            return false;

        return true;
    }

    private PaymentResultDTO CreateErrorResult(PaymentRequestDTO request, string errorMessage)
    {
        return new PaymentResultDTO
        {
            Success = false,
            TransactionId = string.Empty,
            PaymentMethod = "Stripe",
            Amount = request.Amount,
            Currency = request.Currency,
            ProcessedAt = DateTime.UtcNow,
            ErrorMessage = errorMessage
        };
    }
}
