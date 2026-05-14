using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.RobotDTOs
{
    public class RobotLoginDTO
    {
        [Required]
        public string SerialNumber { get; set; } // Unique robot serial number

        [Required]
        public string AccessKey { get; set; } // Secret key for authentication
    }
}
