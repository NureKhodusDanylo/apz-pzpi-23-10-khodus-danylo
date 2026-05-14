namespace Application.DTOs.RobotDTOs
{
    public class RobotResponseDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Model { get; set; }
        public string? SerialNumber { get; set; } // Robot serial number for identification
        public RobotType Type { get; set; }
        public string TypeName { get; set; }
        public RobotStatus Status { get; set; }
        public string StatusName { get; set; }
        public double BatteryLevel { get; set; }
        public double BatteryCapacityJoules { get; set; }
        public double EnergyConsumptionPerMeterJoules { get; set; }
        public double HoverConsumptionPerSecondJoules { get; set; }
        public int? CurrentNodeId { get; set; }
        public string? CurrentNodeName { get; set; }
        public int ActiveOrdersCount { get; set; }
    }
}
