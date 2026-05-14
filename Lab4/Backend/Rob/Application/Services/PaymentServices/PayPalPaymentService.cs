using Application.Abstractions.Interfaces;
using Application.DTOs.PaymentDTOs;
using Entities.Config;
using Microsoft.Extensions.Logging;
using PayPalCheckoutSdk.Core;
using PayPalCheckoutSdk.Orders;
using PayPalHttp;

namespace Application.Services.PaymentServices;

public class PayPalPaymentService : IPaymentService
{
    private readonly PayPalHttpClient _client;
    private readonly ILogger<PayPalPaymentService> _logger;
    private readonly PayPalSettings _settings;

    public PayPalPaymentService(Config config, ILogger<PayPalPaymentService> logger)
    {
        _settings = config.Payment.PayPal;
        _logger = logger;

        // Initialize PayPal environment (Sandbox or Live)
        PayPalEnvironment environment = _settings.Mode.ToLower() == "live"
            ? new LiveEnvironment(_settings.ClientId, _settings.ClientSecret)
            : new SandboxEnvironment(_settings.ClientId, _settings.ClientSecret);

        _client = new PayPalHttpClient(environment);
    }

    public async Task<PaymentResultDTO> ProcessPaymentAsync(PaymentRequestDTO request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.PayPalEmail))
            {
                return CreateErrorResult(request, "PayPal email is required");
            }

            // Create order request
            var orderRequest = new OrdersCreateRequest();
            orderRequest.Prefer("return=representation");
            orderRequest.RequestBody(BuildOrderRequest(request));

            // Execute PayPal API call
            var response = await _client.Execute(orderRequest);
            var result = response.Result<Order>();

            _logger.LogInformation($"PayPal order created: {result.Id}");

            // Auto-capture for now (in production, you'd approve then capture)
            if (result.Status == "CREATED")
            {
                var captureRequest = new OrdersCaptureRequest(result.Id);
                captureRequest.RequestBody(new OrderActionRequest());

                var captureResponse = await _client.Execute(captureRequest);
                var captureResult = captureResponse.Result<Order>();

                if (captureResult.Status == "COMPLETED")
                {
                    return new PaymentResultDTO
                    {
                        Success = true,
                        TransactionId = captureResult.Id,
                        PaymentMethod = "PayPal",
                        Amount = request.Amount,
                        Currency = request.Currency,
                        ProcessedAt = DateTime.UtcNow,
                        ErrorMessage = null
                    };
                }
            }

            return CreateErrorResult(request, $"Payment not completed. Status: {result.Status}");
        }
        catch (HttpException ex)
        {
            _logger.LogError(ex, "PayPal payment failed");
            return CreateErrorResult(request, $"PayPal error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during PayPal payment");
            return CreateErrorResult(request, $"Payment failed: {ex.Message}");
        }
    }

    public async Task<bool> RefundPaymentAsync(string transactionId, decimal amount)
    {
        try
        {
            // PayPal refund is complex and requires capture ID
            // For this implementation, we'll simulate it
            // In production, you'd need to track capture IDs from successful payments

            _logger.LogInformation($"PayPal refund requested for transaction {transactionId}, amount: {amount}");

            // TODO: Implement actual refund when capture IDs are tracked
            // Would need: CapturesRefundRequest from PayPalCheckoutSdk.Payments

            await Task.Delay(100); // Simulate API call

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"PayPal refund failed for transaction {transactionId}");
            return false;
        }
    }

    public async Task<bool> ValidatePaymentDetailsAsync(PaymentRequestDTO request)
    {
        await Task.CompletedTask;

        if (string.IsNullOrEmpty(request.PayPalEmail))
            return false;

        if (!request.PayPalEmail.Contains("@") || request.PayPalEmail.Length < 5)
            return false;

        if (request.Amount <= 0)
            return false;

        return true;
    }

    private OrderRequest BuildOrderRequest(PaymentRequestDTO request)
    {
        return new OrderRequest
        {
            CheckoutPaymentIntent = "CAPTURE",
            PurchaseUnits = new List<PurchaseUnitRequest>
            {
                new PurchaseUnitRequest
                {
                    AmountWithBreakdown = new AmountWithBreakdown
                    {
                        CurrencyCode = request.Currency,
                        Value = request.Amount.ToString("F2")
                    },
                    Description = $"RobDelivery Order #{request.OrderId}"
                }
            },
            ApplicationContext = new ApplicationContext
            {
                BrandName = "RobDelivery",
                LandingPage = "BILLING",
                UserAction = "PAY_NOW"
            }
        };
    }

    private PaymentResultDTO CreateErrorResult(PaymentRequestDTO request, string errorMessage)
    {
        return new PaymentResultDTO
        {
            Success = false,
            TransactionId = string.Empty,
            PaymentMethod = "PayPal",
            Amount = request.Amount,
            Currency = request.Currency,
            ProcessedAt = DateTime.UtcNow,
            ErrorMessage = errorMessage
        };
    }
}
