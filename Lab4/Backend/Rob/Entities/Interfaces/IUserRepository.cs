using Entities.Models;
using System.Threading.Tasks;

namespace Application.Abstractions.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(int userId);
        Task<User> GetByEmailAsync(string email);
        Task<User?> GetByEmailOrPhoneAsync(string emailOrPhone);
        Task<User> GetByGoogleIdAsync(string googleId);
        Task<bool> ExistsByEmailAsync(string email);
        Task<bool> IsPasswordValidByEmailAsync(string email, string passwordHash);
        Task AddAsync(User user);
        Task UpdateAsync(User user);
        Task<IEnumerable<User>> GetAllAsync();
        Task<IEnumerable<User>> SearchUsersAsync(string query);
        Task SaveChangesAsync();
    }
}