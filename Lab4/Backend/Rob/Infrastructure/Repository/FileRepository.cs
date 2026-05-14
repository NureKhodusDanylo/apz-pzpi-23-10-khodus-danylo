using Entities.Interfaces;
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using FileEntity = Entities.Models.File;

namespace Infrastructure.Repository
{
    public class FileRepository : IFileRepository
    {
        private readonly MyDbContext _context;

        public FileRepository(MyDbContext context)
        {
            _context = context;
        }

        public async Task<FileEntity?> GetByIdAsync(int fileId)
        {
            return await _context.Files.FindAsync(fileId);
        }

        public async Task<IEnumerable<FileEntity>> GetByOrderIdAsync(int orderId)
        {
            return await _context.Files
                .Where(f => f.OrderId == orderId)
                .ToListAsync();
        }

        public async Task<FileEntity?> GetUserProfilePhotoAsync(int userId)
        {
            return await _context.Files
                .FirstOrDefaultAsync(f => f.UserId == userId);
        }

        public async Task AddAsync(FileEntity file)
        {
            await _context.Files.AddAsync(file);
        }

        public async Task DeleteAsync(int fileId)
        {
            var file = await _context.Files.FindAsync(fileId);
            if (file != null)
            {
                _context.Files.Remove(file);
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
