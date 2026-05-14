namespace Application.DTOs.RobotDTOs
{
    public class UpdateRobotDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Model { get; set; }
        public RobotType Type { get; set; }
        public RobotStatus Status { get; set; }
        public double BatteryLevel { get; set; }
        public int? CurrentNodeId { get; set; }
    }
}
