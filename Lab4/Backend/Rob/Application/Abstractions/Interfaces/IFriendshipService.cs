using Application.DTOs.FriendshipDTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Abstractions.Interfaces
{
    public interface IFriendshipService
    {
        Task SendFriendRequestAsync(int senderId, SendFriendRequestDTO request);
        Task AcceptFriendRequestAsync(int userId, int requestId);
        Task RejectFriendRequestAsync(int userId, int requestId);
        Task<IEnumerable<FriendDTO>> GetFriendsAsync(int userId);
        Task<IEnumerable<FriendRequestDTO>> GetPendingRequestsAsync(int userId);
        Task RemoveFriendAsync(int userId, int friendId);
    }
}
