namespace Application.DTOs.MapDTOs
{
    public class MapDataResponseDTO
    {
        public List<RobotMapPositionDTO> Robots { get; set; } = new();
        public List<NodeMapPositionDTO> Nodes { get; set; } = new();
    }
}
