using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.UserDTOs
{
    public class CompleteGoogleRegDTO
    {
        [Required]
        public string GoogleId { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string UserName { get; set; }

        [Required]
        public string PhoneNumber { get; set; }

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        public string? Address { get; set; }

        /// <summary>
        /// Optional admin registration key
        /// </summary>
        public string? AdminKey { get; set; }
    }
}
