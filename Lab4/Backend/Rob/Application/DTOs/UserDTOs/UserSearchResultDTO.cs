namespace Application.DTOs.UserDTOs
{
    public class UserSearchResultDTO
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
    }
}
