using Application.Abstractions.Interfaces;
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repository
{
    public class UserRepository(MyDbContext _context) : GenericRepository<User>(_context), IUserRepository
    {
        public async Task<User?> GetByIdAsync(int userId)
        {
            return await _context.Users
                .Include(u => u.SentOrders)
                .Include(u => u.ReceivedOrders)
                .Include(u => u.PersonalNode)
                .Include(u => u.ProfilePhoto)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<User?> GetByIdMainAsync(ulong userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetByEmailOrPhoneAsync(string emailOrPhone)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == emailOrPhone || u.PhoneNumber == emailOrPhone);
        }
        public async Task<User?> GetByGoogleIdAsync(string googleId)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId);
        }
        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
        public async Task DeleteAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
        }
        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _context.Users
                .Include(u => u.SentOrders)
                .Include(u => u.ReceivedOrders)
                .Include(u => u.PersonalNode)
                .Include(u => u.ProfilePhoto)
                .ToListAsync();
        }
        public async Task<bool> ExistsByEmailAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }
        public async Task<bool> ExistsByNameAsync(string name)
        {
            return await _context.Users.AnyAsync(u => u.UserName == name);
        }
        public async Task<bool> IsPasswordValidNasmeAsync(string name, string passwordHash)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == name);

            if (user == null || string.IsNullOrEmpty(user.PasswordHash))
                return false;

            return user.PasswordHash == passwordHash;
        }
        public async Task<bool> IsPasswordValidByEmailAsync(string email, string passwordHash)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null || string.IsNullOrEmpty(user.PasswordHash))
                return false;

            return user.PasswordHash == passwordHash;
        }

        public async Task<IEnumerable<User>> SearchUsersAsync(string query)
        {
            var lowerQuery = query.ToLower();

            return await _context.Users
                .Include(u => u.PersonalNode)
                .Where(u =>
                    u.UserName.ToLower().Contains(lowerQuery) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(query)) ||
                    (u.PersonalNode != null && u.PersonalNode.Name.ToLower().Contains(lowerQuery))
                )
                .ToListAsync();
        }
    }
}
