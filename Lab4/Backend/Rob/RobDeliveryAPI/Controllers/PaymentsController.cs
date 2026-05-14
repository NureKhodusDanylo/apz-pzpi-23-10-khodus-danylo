using Application.Abstractions.Interfaces;
using Application.DTOs.PaymentDTOs;
using Application.DTOs.OrderDTOs;
using Entities.Interfaces;
using Entities.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace RobDeliveryAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentProcessorService _paymentProcessorService;
    private readonly IOrderRepository _orderRepository;
    private readonly IUserRepository _userRepository;

    public PaymentsController(
        IPaymentProcessorService paymentProcessorService,
        IOrderRepository orderRepository,
        IUserRepository userRepository)
    {
        _paymentProcessorService = paymentProcessorService;
        _orderRepository = orderRepository;
        _userRepository = userRepository;
    }

    /// <summary>
    /// Get authenticated user ID from JWT token
    /// </summary>
    private int GetAuthenticatedUserId()
    {
        var userIdClaim = User.FindFirst("Id") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }

    /// <summary>
    /// Process a payment using the specified payment method (PayPal, GooglePay, or Stripe)
    /// </summary>
    [HttpPost("process")]
    public async Task<ActionResult<PaymentResultDTO>> ProcessPayment([FromBody] PaymentRequestDTO request)
    {
        try
        {
            if (request.Amount <= 0)
            {
                return BadRequest(new PaymentResultDTO
                {
                    Success = false,
                    TransactionId = string.Empty,
                    PaymentMethod = request.PaymentMethod,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    ProcessedAt = DateTime.UtcNow,
                    ErrorMessage = "Payment amount must be greater than zero"
                });
            }

            var result = await _paymentProcessorService.ProcessPaymentAsync(request);

            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new PaymentResultDTO
            {
                Success = false,
                TransactionId = string.Empty,
                PaymentMethod = request.PaymentMethod,
                Amount = request.Amount,
                Currency = request.Currency,
                ProcessedAt = DateTime.UtcNow,
                ErrorMessage = ex.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new PaymentResultDTO
            {
                Success = false,
                TransactionId = string.Empty,
                PaymentMethod = request.PaymentMethod,
                Amount = request.Amount,
                Currency = request.Currency,
                ProcessedAt = DateTime.UtcNow,
                ErrorMessage = $"Internal server error: {ex.Message}"
            });
        }
    }

    /// <summary>
    /// Pay for order product and/or delivery
    /// </summary>
    [HttpPost("pay-order")]
    public async Task<ActionResult<PaymentResultDTO>> PayOrder([FromBody] PayOrderDTO paymentDto)
    {
        try
        {
            int userId = GetAuthenticatedUserId();

            // Validate that at least one payment option is selected
            if (!paymentDto.PayProduct && !paymentDto.PayDelivery)
            {
                return BadRequest(new PaymentResultDTO
                {
                    Success = false,
                    ErrorMessage = "At least one payment option (PayProduct or PayDelivery) must be selected",
                    ProcessedAt = DateTime.UtcNow
                });
            }

            // Get order
            var order = await _orderRepository.GetByIdAsync(paymentDto.OrderId);
            if (order == null)
            {
                return NotFound(new PaymentResultDTO
                {
                    Success = false,
                    ErrorMessage = $"Order with ID {paymentDto.OrderId} not found",
                    ProcessedAt = DateTime.UtcNow
                });
            }

            // Validate user authorization
            bool isAuthorized = false;
            if (paymentDto.PayProduct && order.RecipientId == userId)
            {
                isAuthorized = true; // Recipient pays for product
            }
            if (paymentDto.PayDelivery)
            {
                // Check who should pay for delivery
                if ((order.DeliveryPayer == DeliveryPayer.Sender && order.SenderId == userId) ||
                    (order.DeliveryPayer == DeliveryPayer.Recipient && order.RecipientId == userId))
                {
                    isAuthorized = true;
                }
            }

            if (!isAuthorized)
            {
                return StatusCode(403, new PaymentResultDTO
                {
                    Success = false,
                    ErrorMessage = "You are not authorized to make this payment",
                    ProcessedAt = DateTime.UtcNow
                });
            }

            // Calculate total amount to pay
            decimal totalAmount = 0;
            bool willPayProduct = false;
            bool willPayDelivery = false;

            if (paymentDto.PayProduct && !order.IsProductPaid)
            {
                totalAmount += order.ProductPrice;
                willPayProduct = true;
            }
            if (paymentDto.PayDelivery && !order.IsDeliveryPaid)
            {
                totalAmount += order.DeliveryPrice;
                willPayDelivery = true;
            }

            // Check if there's anything to pay
            if (totalAmount == 0)
            {
                if (order.Status == OrderStatus.AwaitingConfirmation && order.IsProductPaid && order.IsDeliveryPaid)
                {
                    // Allow simple confirmation if everything is already paid
                    order.Status = OrderStatus.Pending;
                    await _orderRepository.UpdateAsync(order);
                    
                    return Ok(new PaymentResultDTO
                    {
                        Success = true,
                        TransactionId = "CONFIRM_ONLY",
                        PaymentMethod = paymentDto.PaymentMethod,
                        Amount = 0,
                        Currency = "USD",
                        OrderId = order.Id,
                        ProductPaid = true,
                        DeliveryPaid = true,
                        ProcessedAt = DateTime.UtcNow
                    });
                }

                return BadRequest(new PaymentResultDTO
                {
                    Success = false,
                    ErrorMessage = "The selected items are already paid",
                    OrderId = order.Id,
                    ProductPaid = order.IsProductPaid,
                    DeliveryPaid = order.IsDeliveryPaid,
                    ProcessedAt = DateTime.UtcNow
                });
            }

            var paymentRequest = new PaymentRequestDTO
            {
                Amount = totalAmount,
                Currency = "USD",
                OrderId = order.Id,
                PaymentMethod = paymentDto.PaymentMethod,
                StripeCardToken = paymentDto.StripeCardToken
            };

            var paymentResult = await _paymentProcessorService.ProcessPaymentAsync(paymentRequest);

            if (!paymentResult.Success)
            {
                return BadRequest(new PaymentResultDTO
                {
                    Success = false,
                    ErrorMessage = $"Payment failed: {paymentResult.ErrorMessage}",
                    OrderId = order.Id,
                    ProcessedAt = DateTime.UtcNow
                });
            }

            // Update order payment status
            if (willPayProduct)
            {
                order.IsProductPaid = true;
                // Add ProductPrice to Sender's balance
                var sender = await _userRepository.GetByIdAsync(order.SenderId);
                if (sender != null)
                {
                    sender.Balance += order.ProductPrice;
                    await _userRepository.UpdateAsync(sender);
                }
            }
            if (willPayDelivery)
            {
                order.IsDeliveryPaid = true;
            }

            // Update order status
            if (order.Status == OrderStatus.AwaitingPayment && order.IsDeliveryPaid)
            {
                order.Status = OrderStatus.AwaitingConfirmation;
            }
            if (order.Status == OrderStatus.AwaitingConfirmation && order.IsProductPaid && order.IsDeliveryPaid)
            {
                order.Status = OrderStatus.Pending;
            }

            await _orderRepository.UpdateAsync(order);

            // Return success result
            return Ok(new PaymentResultDTO
            {
                Success = true,
                TransactionId = paymentResult.TransactionId,
                PaymentMethod = paymentResult.PaymentMethod,
                Amount = totalAmount,
                Currency = "UAH",
                ProcessedAt = paymentResult.ProcessedAt,
                OrderId = order.Id,
                ProductPaid = order.IsProductPaid,
                DeliveryPaid = order.IsDeliveryPaid
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new PaymentResultDTO
            {
                Success = false,
                ErrorMessage = ex.Message,
                ProcessedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new PaymentResultDTO
            {
                Success = false,
                ErrorMessage = $"An error occurred while processing payment: {ex.Message}",
                ProcessedAt = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Refund a previously processed payment
    /// </summary>
    [HttpPost("refund")]
    public async Task<ActionResult<object>> RefundPayment([FromBody] RefundRequestDTO request)
    {
        try
        {
            if (request.Amount <= 0)
            {
                return BadRequest(new { success = false, message = "Refund amount must be greater than zero" });
            }

            var result = await _paymentProcessorService.RefundPaymentAsync(
                request.PaymentMethod,
                request.TransactionId,
                request.Amount
            );

            if (result)
            {
                return Ok(new
                {
                    success = true,
                    message = "Refund processed successfully",
                    transactionId = request.TransactionId,
                    amount = request.Amount,
                    processedAt = DateTime.UtcNow
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Refund processing failed"
                });
            }
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}" });
        }
    }
}

public class RefundRequestDTO
{
    public string TransactionId { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
