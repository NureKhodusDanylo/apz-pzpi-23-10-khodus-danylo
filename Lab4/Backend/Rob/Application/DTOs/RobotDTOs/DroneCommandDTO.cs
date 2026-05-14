using Application.DTOs.OrderDTOs;

namespace Application.DTOs.RobotDTOs
{
    /// <summary>
    /// Command packet sent to Arduino drone with order and route information
    /// </summary>
    public class DroneCommandDTO
    {
        public int OrderId { get; set; }
        public string OrderName { get; set; }
        public double PackageWeight { get; set; }

        // Pickup location
        public int PickupNodeId { get; set; }
        public string PickupNodeName { get; set; }
        public double PickupLatitude { get; set; }
        public double PickupLongitude { get; set; }

        // Dropoff location
        public int DropoffNodeId { get; set; }
        public string DropoffNodeName { get; set; }
        public double DropoffLatitude { get; set; }
        public double DropoffLongitude { get; set; }

        // Route information
        public List<RouteWaypointDTO> Route { get; set; }
        public double TotalDistanceMeters { get; set; }
        public double EstimatedBatteryUsagePercent { get; set; }

        // Timestamps
        public DateTime CommandTimestamp { get; set; }
    }

    /// <summary>
    /// Simplified waypoint for Arduino (lightweight)
    /// </summary>
    public class RouteWaypointDTO
    {
        public int SequenceNumber { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Action { get; set; } // "travel", "charge", "pickup", "deliver"
        public double DistanceMeters { get; set; }
    }

    /// <summary>
    /// Response from Arduino drone
    /// </summary>
    public class DroneResponseDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int OrderId { get; set; }
        public double CurrentBatteryLevel { get; set; }
        public string Status { get; set; }
    }
}
