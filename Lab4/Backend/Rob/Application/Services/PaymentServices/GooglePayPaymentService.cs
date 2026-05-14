using Application.Abstractions.Interfaces;
using Application.DTOs.PaymentDTOs;
using Entities.Config;
using Microsoft.Extensions.Logging;
using Stripe;
using System.Text.Json;

namespace Application.Services.PaymentServices;

public class GooglePayPaymentService : IPaymentService
{
    private readonly ILogger<GooglePayPaymentService> _logger;
    private readonly GooglePaySettings _googlePaySettings;
    private readonly StripeSettings _stripeSettings;

    public GooglePayPaymentService(Config config, ILogger<GooglePayPaymentService> logger)
    {
        _googlePaySettings = config.Payment.GooglePay;
        _stripeSettings = config.Payment.Stripe;
        _logger = logger;

        // Set Stripe API key (Google Pay tokens are processed through Stripe)
        StripeConfiguration.ApiKey = _stripeSettings.SecretKey;
    }

    public async Task<PaymentResultDTO> ProcessPaymentAsync(PaymentRequestDTO request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.GooglePayToken))
            {
                return CreateErrorResult(request, "Google Pay token is required");
            }

            // Validate and parse Google Pay token
            if (!ValidateGooglePayToken(request.GooglePayToken))
            {
                return CreateErrorResult(request, "Invalid Google Pay token format");
            }

            // Google Pay tokens are processed through the configured gateway (Stripe in our case)
            var amountInCents = (long)(request.Amount * 100);

            // Create PaymentIntent with Google Pay token
            var paymentIntentService = new PaymentIntentService();
            var paymentIntentOptions = new PaymentIntentCreateOptions
            {
                Amount = amountInCents,
                Currency = request.Currency.ToLower(),
                PaymentMethod = request.GooglePayToken, // Google Pay payment method token
                Confirm = true,
                Description = $"RobDelivery Order #{request.OrderId} (Google Pay)",
                Metadata = new Dictionary<string, string>
                {
                    { "order_id", request.OrderId.ToString() },
                    { "service", "RobDelivery" },
                    { "payment_method", "GooglePay" }
                },
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                    AllowRedirects = "never"
                }
            };

            var paymentIntent = await paymentIntentService.CreateAsync(paymentIntentOptions);

            _logger.LogInformation($"Google Pay payment processed via Stripe: {paymentIntent.Id}, Status: {paymentIntent.Status}");

            if (paymentIntent.Status == "succeeded")
            {
                return new PaymentResultDTO
                {
                    Success = true,
                    TransactionId = paymentIntent.Id,
                    PaymentMethod = "GooglePay",
                    Amount = request.Amount,
                    Currency = request.Currency,
                    ProcessedAt = DateTime.UtcNow,
                    ErrorMessage = null
                };
            }
            else if (paymentIntent.Status == "requires_action")
            {
                return CreateErrorResult(request, "Payment requires additional authentication");
            }
            else
            {
                return CreateErrorResult(request, $"Payment failed with status: {paymentIntent.Status}");
            }
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, $"Google Pay payment failed (Stripe): {ex.StripeError.Message}");
            return CreateErrorResult(request, $"Payment error: {ex.StripeError.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Google Pay payment");
            return CreateErrorResult(request, $"Payment failed: {ex.Message}");
        }
    }

    public async Task<bool> RefundPaymentAsync(string transactionId, decimal amount)
    {
        try
        {
            // Refunds are processed through Stripe (the gateway)
            var refundService = new RefundService();
            var refundOptions = new RefundCreateOptions
            {
                PaymentIntent = transactionId,
                Amount = (long)(amount * 100),
                Reason = RefundReasons.RequestedByCustomer,
                Metadata = new Dictionary<string, string>
                {
                    { "original_method", "GooglePay" }
                }
            };

            var refund = await refundService.CreateAsync(refundOptions);

            _logger.LogInformation($"Google Pay refund completed: {refund.Id}, Status: {refund.Status}");

            return refund.Status == "succeeded" || refund.Status == "pending";
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, $"Google Pay refund failed for transaction {transactionId}: {ex.StripeError.Message}");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Unexpected error during Google Pay refund for transaction {transactionId}");
            return false;
        }
    }

    public async Task<bool> ValidatePaymentDetailsAsync(PaymentRequestDTO request)
    {
        await Task.CompletedTask;

        if (string.IsNullOrEmpty(request.GooglePayToken))
            return false;

        // Google Pay tokens should be valid Stripe payment method IDs or tokens
        if (!ValidateGooglePayToken(request.GooglePayToken))
            return false;

        if (request.Amount <= 0)
            return false;

        return true;
    }

    private bool ValidateGooglePayToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        // Google Pay tokens processed through Stripe start with 'pm_' (Payment Method)
        // or can be a complete payment token starting with 'tok_'
        return token.StartsWith("pm_") || token.StartsWith("tok_") || token.Length > 20;
    }

    private PaymentResultDTO CreateErrorResult(PaymentRequestDTO request, string errorMessage)
    {
        return new PaymentResultDTO
        {
            Success = false,
            TransactionId = string.Empty,
            PaymentMethod = "GooglePay",
            Amount = request.Amount,
            Currency = request.Currency,
            ProcessedAt = DateTime.UtcNow,
            ErrorMessage = errorMessage
        };
    }
}
