using Entities.Models;

namespace Application.Abstractions.Interfaces;

/// <summary>
/// Repository interface for AdminKey entity operations
/// </summary>
public interface IAdminKeyRepository
{
    Task<AdminKey?> GetByIdAsync(int id);
    Task<AdminKey?> GetByKeyCodeAsync(string keyCode);
    Task<IEnumerable<AdminKey>> GetAllAsync();
    Task<IEnumerable<AdminKey>> GetUnusedKeysAsync();
    Task<IEnumerable<AdminKey>> GetKeysByAdminAsync(int adminId);
    Task<bool> IsKeyValidAsync(string keyCode);
    Task AddAsync(AdminKey adminKey);
    Task UpdateAsync(AdminKey adminKey);
    Task SaveChangesAsync();
}
