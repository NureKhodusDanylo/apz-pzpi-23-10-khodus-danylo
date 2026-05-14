using Application.Abstractions.Interfaces;
using Application.DTOs.OrderDTOs;
using Application.DTOs.FileDTOs;
using Entities.Interfaces;
using Entities.Models;
using Entities.Enums;
using Microsoft.Extensions.Logging;
using Application.Abstractions.Exceptions;

namespace Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IUserRepository _userRepository;
        private readonly IRobotRepository _robotRepository;
        private readonly INodeRepository _nodeRepository;
        private readonly IDroneConnectionService _droneConnectionService;
        private readonly ISettingsService _settingsService;
        private readonly IRoutingService _routingService;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            IOrderRepository orderRepository,
            IUserRepository userRepository,
            IRobotRepository robotRepository,
            INodeRepository nodeRepository,
            IDroneConnectionService droneConnectionService,
            ISettingsService settingsService,
            IRoutingService routingService,
            ILogger<OrderService> logger)
        {
            _orderRepository = orderRepository;
            _userRepository = userRepository;
            _robotRepository = robotRepository;
            _nodeRepository = nodeRepository;
            _droneConnectionService = droneConnectionService;
            _settingsService = settingsService;
            _routingService = routingService;
            _logger = logger;
        }

        public async Task<OrderResponseDTO> CreateOrderAsync(int senderId, CreateOrderDTO orderDto)
        {
            // Validate sender exists
            var sender = await _userRepository.GetByIdAsync(senderId);
            if (sender == null)
            {
                throw new ArgumentException("Sender not found");
            }

            // Validate recipient exists
            var recipient = await _userRepository.GetByIdAsync(orderDto.RecipientId);
            if (recipient == null)
            {
                throw new ArgumentException("Recipient not found");
            }

            // Validate that sender and recipient are different
            if (senderId == orderDto.RecipientId)
            {
                throw new ArgumentException("Sender and recipient cannot be the same user");
            }

            // Validate sender has personal node
            if (!sender.PersonalNodeId.HasValue)
            {
                throw new ArgumentException("Sender does not have a personal node configured");
            }

            // Validate recipient has personal node
            if (!recipient.PersonalNodeId.HasValue)
            {
                throw new ArgumentException("Recipient does not have a personal node configured");
            }

            // Validate weight and product price
            if (orderDto.Weight <= 0)
            {
                throw new ArgumentException("Weight must be greater than zero");
            }

            if (orderDto.ProductPrice < 0)
            {
                throw new ArgumentException("Product price cannot be negative");
            }

            // Calculate delivery price based on weight
            // Formula: Base price (50) + weight-based price (10 per kg)
            decimal deliveryPrice = await CalculateDeliveryPriceAsync(orderDto.Weight);

            // Create new order
            var order = new Order
            {
                Name = orderDto.Name,
                Description = orderDto.Description,
                Weight = orderDto.Weight,
                DeliveryPrice = deliveryPrice,
                ProductPrice = orderDto.ProductPrice,
                IsProductPaid = orderDto.IsProductPaid,
                DeliveryPayer = orderDto.DeliveryPayer,
                IsDeliveryPaid = false,
                Status = orderDto.DeliveryPayer == DeliveryPayer.Sender ? OrderStatus.AwaitingPayment : OrderStatus.AwaitingConfirmation,
                SenderId = senderId,
                RecipientId = orderDto.RecipientId,
                PickupNodeId = sender.PersonalNodeId.Value,
                DropoffNodeId = recipient.PersonalNodeId.Value,
                CreatedAt = DateTime.UtcNow
            };

            await _orderRepository.AddAsync(order);

            // Retrieve the created order with all related data
            var createdOrder = await _orderRepository.GetByIdAsync(order.Id);

            return MapToResponseDTO(createdOrder!);
        }

        public async Task<decimal> CalculateDeliveryPriceAsync(double weight)
        {
            var settings = await _settingsService.GetPricingSettingsAsync();

            // Calculate total delivery price
            decimal totalPrice = settings.BasePrice + (decimal)weight * settings.PricePerKg;

            return Math.Round(totalPrice, 2);
        }

        public async Task<OrderResponseDTO?> GetOrderByIdAsync(int orderId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            return order == null ? null : MapToResponseDTO(order);
        }

        public async Task<IEnumerable<OrderResponseDTO>> GetAllOrdersAsync()
        {
            var orders = await _orderRepository.GetAllAsync();
            return orders.Select(MapToResponseDTO);
        }

        public async Task<IEnumerable<OrderResponseDTO>> GetUserOrdersAsync(int userId)
        {
            var orders = await _orderRepository.GetByUserIdAsync(userId);
            return orders.Select(MapToResponseDTO);
        }

        public async Task<IEnumerable<OrderResponseDTO>> GetSentOrdersAsync(int senderId)
        {
            var orders = await _orderRepository.GetBySenderIdAsync(senderId);
            return orders.Select(MapToResponseDTO);
        }

        public async Task<IEnumerable<OrderResponseDTO>> GetReceivedOrdersAsync(int recipientId)
        {
            var orders = await _orderRepository.GetByRecipientIdAsync(recipientId);
            return orders.Select(MapToResponseDTO);
        }

        public async Task<IEnumerable<OrderResponseDTO>> GetOrdersByStatusAsync(OrderStatus status)
        {
            var orders = await _orderRepository.GetByStatusAsync(status);
            return orders.Select(MapToResponseDTO);
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
            {
                return false;
            }

            // Validate status transition
            if (!IsValidStatusTransition(order.Status, newStatus))
            {
                throw new InvalidOperationException($"Invalid status transition from {order.Status} to {newStatus}");
            }

            order.Status = newStatus;

            // Set completion time if order is delivered or cancelled
            if (newStatus == OrderStatus.Delivered || newStatus == OrderStatus.Cancelled)
            {
                order.CompletedAt = DateTime.UtcNow;
            }

            await _orderRepository.UpdateAsync(order);
            return true;
        }

        public async Task<bool> AssignRobotToOrderAsync(int orderId, int robotId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
            {
                return false;
            }

            // Validate robot exists
            var robot = await _robotRepository.GetByIdAsync(robotId);
            if (robot == null)
            {
                throw new ArgumentException($"Robot with ID {robotId} does not exist");
            }

            // Check if order can be assigned
            if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Processing)
            {
                throw new InvalidOperationException($"Cannot assign robot to order with status {order.Status}");
            }

            // Assign robot to order
            order.RobotId = robotId;

            // Update order status to Processing if it was Pending
            if (order.Status == OrderStatus.Pending)
            {
                order.Status = OrderStatus.Processing;
            }

            // Update robot status to Delivering
            robot.Status = RobotStatus.Delivering;
            robot.CurrentNodeId = order.PickupNodeId;

            await _orderRepository.UpdateAsync(order);
            await _robotRepository.UpdateAsync(robot);

            return true;
        }

        public async Task<bool> CancelOrderAsync(int orderId, int userId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
            {
                return false;
            }

            // Only sender can cancel the order and only if it's not already in progress
            if (order.SenderId != userId)
            {
                throw new UnauthorizedAccessException("Only the sender can cancel the order");
            }

            if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Processing)
            {
                throw new InvalidOperationException("Cannot cancel order that is already en route or completed");
            }

            // If robot was assigned, set it back to Idle
            if (order.RobotId.HasValue)
            {
                var robot = await _robotRepository.GetByIdAsync(order.RobotId.Value);
                if (robot != null)
                {
                    robot.Status = RobotStatus.Idle;
                    await _robotRepository.UpdateAsync(robot);
                }
            }

            order.Status = OrderStatus.Cancelled;
            order.CompletedAt = DateTime.UtcNow;

            await _orderRepository.UpdateAsync(order);
            return true;
        }

        public async Task<bool> DeleteOrderAsync(int orderId)
        {
            if (!await _orderRepository.ExistsAsync(orderId))
            {
                return false;
            }

            await _orderRepository.DeleteAsync(orderId);
            return true;
        }

        private static OrderResponseDTO MapToResponseDTO(Order order)
        {
            return new OrderResponseDTO
            {
                Id = order.Id,
                Name = order.Name,
                Description = order.Description,
                Weight = order.Weight,
                DeliveryPrice = order.DeliveryPrice,
                ProductPrice = order.ProductPrice,
                IsProductPaid = order.IsProductPaid,
                DeliveryPayer = order.DeliveryPayer,
                DeliveryPayerName = order.DeliveryPayer.ToString(),
                IsDeliveryPaid = order.IsDeliveryPaid,
                Status = order.Status,
                CreatedAt = order.CreatedAt,
                CompletedAt = order.CompletedAt,
                SenderId = order.SenderId,
                SenderName = order.Sender?.UserName ?? "Unknown",
                RecipientId = order.RecipientId,
                RecipientName = order.Recipient?.UserName ?? "Unknown",
                RobotId = order.RobotId,
                RobotName = order.AssignedRobot?.Name,
                PickupNodeId = order.PickupNodeId,
                PickupNodeName = order.PickupNode?.Name ?? "Unknown",
                DropoffNodeId = order.DropoffNodeId,
                DropoffNodeName = order.DropoffNode?.Name ?? "Unknown",
                Images = order.Images?.Select(img => new FileResponseDTO
                {
                    Id = img.Id,
                    FileName = img.FileName,
                    FilePath = img.FilePath,
                    ContentType = img.ContentType,
                    FileSize = img.FileSize,
                    UploadedAt = img.UploadedAt
                }).ToList()
            };
        }

        private static bool IsValidStatusTransition(OrderStatus currentStatus, OrderStatus newStatus)
        {
            // Define valid status transitions
            return currentStatus switch
            {
                OrderStatus.AwaitingPayment => newStatus == OrderStatus.AwaitingConfirmation || newStatus == OrderStatus.Cancelled,
                OrderStatus.AwaitingConfirmation => newStatus == OrderStatus.Pending || newStatus == OrderStatus.Cancelled,
                OrderStatus.Pending => newStatus == OrderStatus.Processing || newStatus == OrderStatus.Cancelled,
                OrderStatus.Processing => newStatus == OrderStatus.EnRoute || newStatus == OrderStatus.Cancelled,
                OrderStatus.EnRoute => newStatus == OrderStatus.Delivered || newStatus == OrderStatus.Cancelled,
                OrderStatus.Delivered => false, // Cannot change from delivered
                OrderStatus.Cancelled => false, // Cannot change from cancelled
                _ => false
            };
        }

        public async Task<ExecuteOrderResponseDTO> ExecuteOrderAsync(int orderId, int userId)
        {
            var globalLogs = new List<string>();
            globalLogs.Add($"[SYSTEM] Execution started at {DateTime.UtcNow}");

            // Get the order
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
            {
                globalLogs.Add($"[ERROR] Order {orderId} not found");
                throw new ArgumentException($"Order with ID {orderId} not found");
            }

            if (!(await _orderRepository.DoesItBelong(orderId, userId)))
            {
                globalLogs.Add($"[ERROR] Unauthorized access for user {userId}");
                throw new InvalidOperationException($"Cannot execute order. Only sender's orders can be executed.");
            }

            // Validate order status
            if (order.Status != OrderStatus.Pending)
            {
                globalLogs.Add($"[ERROR] Order status is {order.Status}, must be Pending");
                throw new InvalidOperationException($"Cannot execute order with status {order.Status}. Only Pending orders can be executed.");
            }

            if (!order.IsProductPaid || !order.IsDeliveryPaid)
            {
                globalLogs.Add($"[ERROR] Payment pending: ProductPaid={order.IsProductPaid}, DeliveryPaid={order.IsDeliveryPaid}");
                throw new InvalidOperationException("Cannot execute order until both product and delivery are paid.");
            }

            // Get pickup and dropoff nodes
            var pickupNode = await _nodeRepository.GetByIdAsync(order.PickupNodeId);
            var dropoffNode = await _nodeRepository.GetByIdAsync(order.DropoffNodeId);

            if (pickupNode == null || dropoffNode == null)
            {
                globalLogs.Add($"[ERROR] Critical node missing. Pickup={order.PickupNodeId}, Dropoff={order.DropoffNodeId}");
                throw new InvalidOperationException("Pickup or dropoff node not found");
            }

            // Get all available drones (Idle and Charging)
            var availableDrones = await _robotRepository.GetAvailableRobotsAsync();
            var dronesList = availableDrones.Where(r => r.Type == RobotType.Drone).ToList();
            globalLogs.Add($"[SYSTEM] Found {dronesList.Count} available drones in the vicinity.");

            if (!dronesList.Any())
            {
                globalLogs.Add("[ERROR] No drones are currently Idle or Charging. Try again when drones finish their missions.");
                var failedResponse = new ExecuteOrderResponseDTO { Message = "No drones available", DebugLogs = globalLogs };
                // We'll throw but maybe the controller can handle this to return the logs
                throw new RoutingException("No suitable drones found (all are offline, busy, or low battery)", globalLogs);
            }

            // Find optimal drone and route
            Robot? optimalDrone = null;
            List<RouteSegmentDTO> optimalRoute = null;
            double minTotalDistance = double.MaxValue;
            double optimalBatteryUsage = 0;

            foreach (var drone in dronesList)
            {
                globalLogs.Add($"\n--- ANALYZING DRONE: {drone.Name} (ID:{drone.Id}) ---");
                
                Node startNode = drone.CurrentNode;
                if (startNode == null && drone.CurrentLatitude.HasValue && drone.CurrentLongitude.HasValue)
                {
                    startNode = new Node 
                    { 
                        Name = $"Drone Position", 
                        Latitude = drone.CurrentLatitude.Value, 
                        Longitude = drone.CurrentLongitude.Value,
                        Type = NodeType.Other
                    };
                }

                if (startNode == null)
                {
                    globalLogs.Add($"[SKIP] Drone has no reported location (GPS/Node).");
                    continue;
                }

                var (canComplete, route, totalDistance, batteryUsage, droneLogs) = await _routingService.CalculateDroneRouteAsync(
                    drone,
                    startNode,
                    pickupNode,
                    dropoffNode,
                    order.Weight
                );

                globalLogs.AddRange(droneLogs);

                if (canComplete && totalDistance < minTotalDistance)
                {
                    optimalDrone = drone;
                    optimalRoute = route;
                    minTotalDistance = totalDistance;
                    optimalBatteryUsage = batteryUsage;
                    globalLogs.Add($"[MATCH] Drone {drone.Name} is currently the best candidate.");
                }
            }

            if (optimalDrone == null || optimalRoute == null)
            {
                globalLogs.Add("\n[CRITICAL] After evaluating all candidates, none could fulfill the route requirements.");
                throw new RoutingException("No suitable drone found that can complete this delivery", globalLogs);
            }

            // Assign the optimal drone to the order
            await AssignRobotToOrderAsync(orderId, optimalDrone.Id);
            globalLogs.Add($"\n[SUCCESS] Assigned to {optimalDrone.Name}. Total Distance: {Math.Round(minTotalDistance, 0)}m");

            return new ExecuteOrderResponseDTO
            {
                OrderId = orderId,
                AssignedRobotId = optimalDrone.Id,
                AssignedRobotName = optimalDrone.Name,
                Message = "Order successfully assigned to optimal drone.",
                Route = optimalRoute,
                TotalDistanceMeters = minTotalDistance,
                EstimatedBatteryUsagePercent = optimalBatteryUsage,
                DebugLogs = globalLogs
            };
        }


    }
}
