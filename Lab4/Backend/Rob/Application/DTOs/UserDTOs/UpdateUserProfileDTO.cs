namespace Application.DTOs.UserDTOs
{
    public class UpdateUserProfileDTO
    {
        public string? UserName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Password { get; set; } // New password if user wants to change it
    }
}
