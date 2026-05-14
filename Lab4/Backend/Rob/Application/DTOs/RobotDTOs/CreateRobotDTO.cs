namespace Application.DTOs.RobotDTOs
{
    public class CreateRobotDTO
    {
        public string Name { get; set; }
        public string Model { get; set; }
        public RobotType Type { get; set; }
        public int? CurrentNodeId { get; set; }
    }
}
