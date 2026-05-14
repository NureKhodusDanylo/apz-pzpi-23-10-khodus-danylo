using System.ComponentModel.DataAnnotations;

namespace Entities.Models
{
    public class SystemSetting
    {
        [Key]
        public string Key { get; set; } = string.Empty;
        
        [Required]
        public string Value { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
