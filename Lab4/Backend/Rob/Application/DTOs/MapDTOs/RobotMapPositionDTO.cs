namespace Application.DTOs.MapDTOs
{
    public class RobotMapPositionDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Model { get; set; }
        public RobotType Type { get; set; }
        public string TypeName { get; set; }
        public RobotStatus Status { get; set; }
        public string StatusName { get; set; }
        public double BatteryLevel { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public int? CurrentNodeId { get; set; }
        public string? CurrentNodeName { get; set; }
        public int? TargetNodeId { get; set; }
        public string? TargetNodeName { get; set; }
        public int ActiveOrdersCount { get; set; }
        public List<Application.DTOs.OrderDTOs.RouteSegmentDTO>? Route { get; set; }
    }
}
