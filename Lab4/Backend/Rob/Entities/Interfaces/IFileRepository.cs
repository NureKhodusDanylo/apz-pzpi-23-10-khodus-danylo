using Entities.Models;
using FileEntity = Entities.Models.File;

namespace Entities.Interfaces
{
    public interface IFileRepository
    {
        Task<FileEntity?> GetByIdAsync(int fileId);
        Task<IEnumerable<FileEntity>> GetByOrderIdAsync(int orderId);
        Task<FileEntity?> GetUserProfilePhotoAsync(int userId);
        Task AddAsync(FileEntity file);
        Task DeleteAsync(int fileId);
        Task SaveChangesAsync();
    }
}
