using Application.Abstractions.Interfaces;
using Application.DTOs.FriendshipDTOs;
using Entities.Interfaces;
using Entities.Models;
using Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services
{
    public class FriendshipService : IFriendshipService
    {
        private readonly IFriendshipRepository _friendshipRepository;
        private readonly IUserRepository _userRepository;

        public FriendshipService(IFriendshipRepository friendshipRepository, IUserRepository userRepository)
        {
            _friendshipRepository = friendshipRepository;
            _userRepository = userRepository;
        }

        public async Task SendFriendRequestAsync(int senderId, SendFriendRequestDTO request)
        {
            User? targetUser = null;

            if (request.TargetUserId.HasValue)
            {
                targetUser = await _userRepository.GetByIdAsync(request.TargetUserId.Value);
            }
            else if (!string.IsNullOrEmpty(request.TargetEmailOrPhone))
            {
                targetUser = await _userRepository.GetByEmailOrPhoneAsync(request.TargetEmailOrPhone);
            }

            if (targetUser == null)
            {
                throw new ArgumentException("User not found");
            }

            if (targetUser.Id == senderId)
            {
                throw new ArgumentException("You cannot add yourself as a friend");
            }

            var existingFriendship = await _friendshipRepository.GetFriendshipAsync(senderId, targetUser.Id);
            if (existingFriendship != null)
            {
                if (existingFriendship.Status == FriendshipStatus.Accepted)
                {
                    throw new ArgumentException("You are already friends");
                }
                if (existingFriendship.UserId == senderId)
                {
                    throw new ArgumentException("Friend request already sent");
                }
                // If the other user sent a request, we can accept it instead of sending a new one?
                // For simplicity, let's just say it's pending.
                throw new ArgumentException("Friend request already exists");
            }

            var friendship = new Friendship
            {
                UserId = senderId,
                FriendId = targetUser.Id,
                Status = FriendshipStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _friendshipRepository.AddAsync(friendship);
            await _friendshipRepository.SaveChangesAsync();
        }

        public async Task AcceptFriendRequestAsync(int userId, int requestId)
        {
            var friendship = await _friendshipRepository.GetByIdAsync(requestId);
            if (friendship == null || friendship.FriendId != userId || friendship.Status != FriendshipStatus.Pending)
            {
                throw new ArgumentException("Friend request not found");
            }

            friendship.Status = FriendshipStatus.Accepted;
            await _friendshipRepository.UpdateAsync(friendship);
            await _friendshipRepository.SaveChangesAsync();
        }

        public async Task RejectFriendRequestAsync(int userId, int requestId)
        {
            var friendship = await _friendshipRepository.GetByIdAsync(requestId);
            if (friendship == null || friendship.FriendId != userId || friendship.Status != FriendshipStatus.Pending)
            {
                throw new ArgumentException("Friend request not found");
            }

            friendship.Status = FriendshipStatus.Rejected;
            await _friendshipRepository.UpdateAsync(friendship);
            await _friendshipRepository.SaveChangesAsync();
        }

        public async Task<IEnumerable<FriendDTO>> GetFriendsAsync(int userId)
        {
            var friendships = await _friendshipRepository.GetUserFriendshipsAsync(userId);
            
            return friendships.Select(f => {
                var friend = f.UserId == userId ? f.Friend : f.User;
                return new FriendDTO
                {
                    Id = friend.Id,
                    UserName = friend.UserName,
                    Email = friend.Email,
                    PhoneNumber = friend.PhoneNumber,
                    Latitude = friend.PersonalNode?.Latitude,
                    Longitude = friend.PersonalNode?.Longitude,
                    Address = friend.PersonalNode?.Name,
                    ProfilePhotoUrl = friend.ProfilePhotoId.HasValue ? $"/api/User/{friend.Id}/photo" : null
                };
            }).ToList();
        }

        public async Task<IEnumerable<FriendRequestDTO>> GetPendingRequestsAsync(int userId)
        {
            var requests = await _friendshipRepository.GetPendingRequestsAsync(userId);

            return requests.Select(r => new FriendRequestDTO
            {
                Id = r.Id,
                SenderId = r.UserId,
                SenderName = r.User.UserName,
                SenderEmail = r.User.Email,
                SenderPhotoUrl = r.User.ProfilePhotoId.HasValue ? $"/api/User/{r.UserId}/photo" : null,
                Status = r.Status,
                CreatedAt = r.CreatedAt
            }).ToList();
        }

        public async Task RemoveFriendAsync(int userId, int friendId)
        {
            var friendship = await _friendshipRepository.GetFriendshipAsync(userId, friendId);
            if (friendship == null)
            {
                throw new ArgumentException("Friendship not found");
            }

            await _friendshipRepository.DeleteAsync(friendship);
            await _friendshipRepository.SaveChangesAsync();
        }
    }
}
