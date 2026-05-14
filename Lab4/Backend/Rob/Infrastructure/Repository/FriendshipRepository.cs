using Entities.Interfaces;
using Entities.Models;
using Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository
{
    public class FriendshipRepository : IFriendshipRepository
    {
        private readonly MyDbContext _context;

        public FriendshipRepository(MyDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Friendship friendship)
        {
            await _context.AddAsync(friendship);
        }

        public async Task<Friendship?> GetByIdAsync(int id)
        {
            return await _context.Set<Friendship>().FindAsync(id);
        }

        public async Task UpdateAsync(Friendship friendship)
        {
            _context.Update(friendship);
        }

        public async Task DeleteAsync(Friendship friendship)
        {
            _context.Remove(friendship);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Friendship>> GetUserFriendshipsAsync(int userId)
        {
            return await _context.Set<Friendship>()
                .Include(f => f.User)
                .Include(f => f.Friend)
                    .ThenInclude(u => u.PersonalNode)
                .Include(f => f.Friend)
                    .ThenInclude(u => u.ProfilePhoto)
                .Where(f => (f.UserId == userId || f.FriendId == userId) && f.Status == FriendshipStatus.Accepted)
                .ToListAsync();
        }

        public async Task<IEnumerable<Friendship>> GetPendingRequestsAsync(int userId)
        {
            return await _context.Set<Friendship>()
                .Include(f => f.User)
                    .ThenInclude(u => u.ProfilePhoto)
                .Where(f => f.FriendId == userId && f.Status == FriendshipStatus.Pending)
                .ToListAsync();
        }

        public async Task<Friendship?> GetFriendshipAsync(int userId, int friendId)
        {
            return await _context.Set<Friendship>()
                .FirstOrDefaultAsync(f =>
                    (f.UserId == userId && f.FriendId == friendId) ||
                    (f.UserId == friendId && f.FriendId == userId));
        }
    }
}
