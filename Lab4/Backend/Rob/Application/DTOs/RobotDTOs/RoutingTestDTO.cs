using Application.DTOs.OrderDTOs;

namespace Application.DTOs.RobotDTOs
{
    public class RoutingTestDTO
    {
        public int RobotId { get; set; }
        public int PickupNodeId { get; set; }
        public int DropoffNodeId { get; set; }
        public double PackageWeight { get; set; }
    }

    public class RoutingTestResponseDTO
    {
        public bool Success { get; set; }
        public double TotalDistanceMeters { get; set; }
        public double EstimatedBatteryUsagePercent { get; set; }
        public List<string> Logs { get; set; } = new();
        public List<RouteSegmentDTO> Route { get; set; } = new();
    }
}
