namespace Application.DTOs.OrderDTOs
{
    public class ExecuteOrderResponseDTO
    {
        public int OrderId { get; set; }
        public int AssignedRobotId { get; set; }
        public string AssignedRobotName { get; set; }
        public string Message { get; set; }
        public List<RouteSegmentDTO> Route { get; set; }
        public double TotalDistanceMeters { get; set; }
        public double EstimatedBatteryUsagePercent { get; set; }
        public List<string> DebugLogs { get; set; } = new List<string>();
    }

    public class RouteSegmentDTO
    {
        public int SegmentNumber { get; set; }
        public string FromNodeName { get; set; }
        public string ToNodeName { get; set; }
        public double FromLatitude { get; set; }
        public double FromLongitude { get; set; }
        public double ToLatitude { get; set; }
        public double ToLongitude { get; set; }
        public double DistanceMeters { get; set; }
        public string Action { get; set; } // "Travel", "Charge", "PickupPackage", "DeliverPackage"
    }
}
