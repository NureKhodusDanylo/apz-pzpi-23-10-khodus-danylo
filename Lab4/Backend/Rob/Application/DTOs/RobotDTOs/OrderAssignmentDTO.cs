namespace Application.DTOs.RobotDTOs
{
    /// <summary>
    /// DTO for sending order assignment to robot/drone
    /// Used when robot polls for assigned orders
    /// </summary>
    public class OrderAssignmentDTO
    {
        public int OrderId { get; set; }
        public string OrderName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Weight { get; set; }

        // Pickup node info
        public int PickupNodeId { get; set; }
        public string PickupNodeName { get; set; } = string.Empty;
        public double PickupLatitude { get; set; }
        public double PickupLongitude { get; set; }

        // Dropoff node info
        public int DropoffNodeId { get; set; }
        public string DropoffNodeName { get; set; } = string.Empty;
        public double DropoffLatitude { get; set; }
        public double DropoffLongitude { get; set; }

        // Route information
        public List<RouteWaypointDTO> Route { get; set; } = new();

        // Estimates
        public double TotalDistanceMeters { get; set; }
        public double EstimatedBatteryUsagePercent { get; set; }

        // Status
        public string OrderStatus { get; set; } = string.Empty;
        public DateTime AssignedAt { get; set; }
    }
}
