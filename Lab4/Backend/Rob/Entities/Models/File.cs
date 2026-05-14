using Entities.Interfaces;

namespace Entities.Models
{
    public class File : IDbEntity
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string ContentType { get; set; }
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public int? OrderId { get; set; }
        public virtual Order? Order { get; set; }

        public int? UserId { get; set; }
        public virtual User? User { get; set; }
    }
}
