using Application.Abstractions.Interfaces;
using Application.DTOs.MapDTOs;
using Entities.Interfaces;
using Entities.Models;
using Entities.Enums;
using System.Linq;

namespace Application.Services
{
    public class MapService : IMapService
    {
        private readonly IRobotRepository _robotRepository;
        private readonly INodeRepository _nodeRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IUserRepository _userRepository;
        private readonly IRoutingService _routingService;

        public MapService(
            IRobotRepository robotRepository, 
            INodeRepository nodeRepository,
            IOrderRepository orderRepository,
            IUserRepository userRepository,
            IRoutingService routingService)
        {
            _robotRepository = robotRepository;
            _nodeRepository = nodeRepository;
            _orderRepository = orderRepository;
            _userRepository = userRepository;
            _routingService = routingService;
        }

        public async Task<MapDataResponseDTO> GetMapDataAsync(int? userId = null)
        {
            var robots = await _robotRepository.GetAllAsync();
            var nodes = await _nodeRepository.GetAllAsync();
            var orders = await _orderRepository.GetAllAsync();
            var user = userId.HasValue ? await _userRepository.GetByIdAsync(userId.Value) : null;
            bool isAdmin = user?.Role == Entities.Enums.UserRole.Admin;

            var robotsList = robots.ToList();
            var nodesList = nodes.ToList();
            var activeOrders = orders.Where(o => o.Status == OrderStatus.Processing || o.Status == OrderStatus.EnRoute).ToList();

            // Count robots at each node
            var robotsAtNodes = robotsList
                .Where(r => r.CurrentNodeId.HasValue)
                .GroupBy(r => r.CurrentNodeId!.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            var robotDTOs = new List<RobotMapPositionDTO>();
            foreach (var robot in robotsList)
            {
                var dto = MapRobotToPosition(robot);
                
                // If robot is delivering, check if we should show the route
                var robotActiveOrder = activeOrders.FirstOrDefault(o => o.RobotId == robot.Id);
                if (robotActiveOrder != null)
                {
                    bool shouldShowRoute = isAdmin || (userId.HasValue && (robotActiveOrder.SenderId == userId.Value || robotActiveOrder.RecipientId == userId.Value));
                    
                    if (shouldShowRoute)
                    {
                        var startNode = robot.CurrentNode ?? nodesList.FirstOrDefault(n => n.Id == robot.CurrentNodeId);
                        
                        // If robot is in flight (between nodes), create a temporary node for routing
                        if (startNode == null && robot.CurrentLatitude.HasValue && robot.CurrentLongitude.HasValue)
                        {
                            startNode = new Node 
                            { 
                                Latitude = robot.CurrentLatitude.Value, 
                                Longitude = robot.CurrentLongitude.Value,
                                Name = "In-Flight Position",
                                Type = NodeType.Other // Changed from non-existent DeliveryPoint
                            };
                        }

                        if (startNode != null)
                        {
                            var (success, route, _, _, _) = await _routingService.CalculateDroneRouteAsync(
                                robot,
                                startNode,
                                robotActiveOrder.PickupNode,
                                robotActiveOrder.DropoffNode,
                                robotActiveOrder.Weight
                            );
                            
                            if (success)
                            {
                                dto.Route = route;
                            }
                        }
                    }
                }
                
                robotDTOs.Add(dto);
            }

            return new MapDataResponseDTO
            {
                Robots = robotDTOs,
                Nodes = nodesList.Select(n => MapNodeToPosition(n, robotsAtNodes.GetValueOrDefault(n.Id, 0))).ToList()
            };
        }

        private RobotMapPositionDTO MapRobotToPosition(Robot robot)
        {
            double? latitude = robot.CurrentLatitude;
            double? longitude = robot.CurrentLongitude;

            // If robot is at a node and doesn't have custom coordinates, use node coordinates
            if ((!latitude.HasValue || !longitude.HasValue) && robot.CurrentNode != null)
            {
                latitude = robot.CurrentNode.Latitude;
                longitude = robot.CurrentNode.Longitude;
            }

            return new RobotMapPositionDTO
            {
                Id = robot.Id,
                Name = robot.Name,
                Model = robot.Model,
                Type = robot.Type,
                TypeName = robot.Type.ToString(),
                Status = robot.Status,
                StatusName = robot.Status.ToString(),
                BatteryLevel = robot.BatteryLevel,
                Latitude = latitude,
                Longitude = longitude,
                CurrentNodeId = robot.CurrentNodeId,
                CurrentNodeName = robot.CurrentNode?.Name,
                TargetNodeId = robot.TargetNodeId,
                TargetNodeName = robot.TargetNode?.Name,
                ActiveOrdersCount = robot.ActiveOrders?.Count ?? 0
            };
        }

        private NodeMapPositionDTO MapNodeToPosition(Node node, int robotsAtNode)
        {
            return new NodeMapPositionDTO
            {
                Id = node.Id,
                Name = node.Name,
                Latitude = node.Latitude,
                Longitude = node.Longitude,
                Type = node.Type,
                TypeName = node.Type.ToString(),
                RobotsAtNode = robotsAtNode
            };
        }
    }
}
