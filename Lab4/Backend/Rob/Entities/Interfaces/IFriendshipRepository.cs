using Entities.Models;

namespace Entities.Interfaces
{
    public interface IFriendshipRepository
    {
        Task AddAsync(Friendship friendship);
        Task<Friendship?> GetByIdAsync(int id);
        Task UpdateAsync(Friendship friendship);
        Task DeleteAsync(Friendship friendship);
        Task SaveChangesAsync();

        Task<IEnumerable<Friendship>> GetUserFriendshipsAsync(int userId);
        Task<IEnumerable<Friendship>> GetPendingRequestsAsync(int userId);
        Task<Friendship?> GetFriendshipAsync(int userId, int friendId);
    }
}
