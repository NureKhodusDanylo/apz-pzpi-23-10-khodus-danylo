namespace Application.DTOs.NodeDTOs
{
    public class CreateNodeDTO
    {
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public NodeType Type { get; set; }
    }
}
