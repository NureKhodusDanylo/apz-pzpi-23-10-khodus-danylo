using Application.DTOs.OrderDTOs;
using Entities.Models;

namespace Application.Abstractions.Interfaces
{
    public interface IRoutingService
    {
        Task<(bool CanComplete, List<RouteSegmentDTO> Route, double TotalDistance, double BatteryUsage, List<string> Logs)> CalculateDroneRouteAsync(
            Robot drone,
            Node currentNode,
            Node pickupNode,
            Node dropoffNode,
            double packageWeight);
            
        double CalculateDistance(Node node1, Node node2);
        double CalculateDistance(double lat1, double lon1, double lat2, double lon2);
    }
}
