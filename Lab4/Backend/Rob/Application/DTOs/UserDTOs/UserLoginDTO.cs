using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.UserDTOs
{
    public class UserLoginDTO
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? googleJwtToken { get; set; }
        public string? PhoneNumber { get; set; }
    }
}
