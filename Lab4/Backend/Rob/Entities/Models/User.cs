using Entities.Enums;
using Entities.Interfaces;
using System.Collections.Generic;

namespace Entities.Models
{
    public class User : IDbEntity
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string? GoogleId { get; set; }
        public string? PhoneNumber { get; set; }
        public string PasswordHash { get; set; }
        public UserRole? Role { get; set; }
        
        public decimal Balance { get; set; } = 0;

        // User's personal location node
        public int? PersonalNodeId { get; set; }
        public virtual Node? PersonalNode { get; set; }

        // Profile photo
        public int? ProfilePhotoId { get; set; }
        public virtual File? ProfilePhoto { get; set; }

        public virtual ICollection<Order> SentOrders { get; set; }
        public virtual ICollection<Order> ReceivedOrders { get; set; }

        public virtual ICollection<Friendship> SentFriendRequests { get; set; } = new List<Friendship>();
        public virtual ICollection<Friendship> ReceivedFriendRequests { get; set; } = new List<Friendship>();
    }
}
