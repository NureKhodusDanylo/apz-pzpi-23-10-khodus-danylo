using Entities.Interfaces;

namespace Entities.Models
{
    public class Node : IDbEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public NodeType Type { get; set; }
    }
}
