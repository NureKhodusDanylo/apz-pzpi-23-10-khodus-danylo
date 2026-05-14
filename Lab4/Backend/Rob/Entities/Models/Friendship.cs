using Entities.Enums;
using Entities.Interfaces;
using System;

namespace Entities.Models
{
    public class Friendship : IDbEntity
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public virtual User User { get; set; }

        public int FriendId { get; set; }
        public virtual User Friend { get; set; }

        public FriendshipStatus Status { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
