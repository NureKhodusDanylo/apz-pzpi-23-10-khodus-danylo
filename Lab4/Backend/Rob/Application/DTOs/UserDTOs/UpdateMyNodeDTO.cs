using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.UserDTOs
{
    public class UpdateMyNodeDTO
    {
        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        public string? Address { get; set; } // Optional address description
    }
}
