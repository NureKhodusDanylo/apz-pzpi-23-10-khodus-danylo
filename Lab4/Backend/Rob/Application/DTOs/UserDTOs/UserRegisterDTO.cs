using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.UserDTOs
{
    public class UserRegisterDTO
    {
        public string? UserName { get; set; }

        public string? Email { get; set; }

        public string? Password { get; set; }
        public string? googleJwtToken { get; set; }
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// Optional admin registration key. If provided and valid, user will be registered as Admin
        /// </summary>
        public string? AdminKey { get; set; }

        // User's personal location (creates a personal Node)
        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        public string? Address { get; set; } // Optional address description
    }
}
