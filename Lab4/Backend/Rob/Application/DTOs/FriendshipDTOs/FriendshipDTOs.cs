using Entities.Enums;

namespace Application.DTOs.FriendshipDTOs
{
    public class FriendDTO
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? Address { get; set; }
        public string? ProfilePhotoUrl { get; set; }
    }

    public class FriendRequestDTO
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; }
        public string? SenderEmail { get; set; }
        public string? SenderPhotoUrl { get; set; }
        public FriendshipStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SendFriendRequestDTO
    {
        public string? TargetEmailOrPhone { get; set; }
        public int? TargetUserId { get; set; }
    }
}
