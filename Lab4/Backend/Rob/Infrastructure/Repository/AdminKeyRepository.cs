using Application.Abstractions.Interfaces;
using Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository;

/// <summary>
/// Repository implementation for AdminKey entity
/// </summary>
public class AdminKeyRepository : GenericRepository<AdminKey>, IAdminKeyRepository
{
    public AdminKeyRepository(MyDbContext context) : base(context)
    {
    }

    public async Task<AdminKey?> GetByIdAsync(int id)
    {
        return await _context.AdminKeys
            .Include(ak => ak.CreatedByAdmin)
            .Include(ak => ak.UsedByUser)
            .FirstOrDefaultAsync(ak => ak.Id == id);
    }

    public async Task<AdminKey?> GetByKeyCodeAsync(string keyCode)
    {
        return await _context.AdminKeys
            .Include(ak => ak.CreatedByAdmin)
            .Include(ak => ak.UsedByUser)
            .FirstOrDefaultAsync(ak => ak.KeyCode == keyCode);
    }

    public async Task<IEnumerable<AdminKey>> GetAllAsync()
    {
        return await _context.AdminKeys
            .Include(ak => ak.CreatedByAdmin)
            .Include(ak => ak.UsedByUser)
            .OrderByDescending(ak => ak.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<AdminKey>> GetUnusedKeysAsync()
    {
        return await _context.AdminKeys
            .Include(ak => ak.CreatedByAdmin)
            .Where(ak => !ak.IsUsed)
            .OrderByDescending(ak => ak.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<AdminKey>> GetKeysByAdminAsync(int adminId)
    {
        return await _context.AdminKeys
            .Include(ak => ak.UsedByUser)
            .Where(ak => ak.CreatedByAdminId == adminId)
            .OrderByDescending(ak => ak.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> IsKeyValidAsync(string keyCode)
    {
        var key = await _context.AdminKeys
            .FirstOrDefaultAsync(ak => ak.KeyCode == keyCode);

        if (key == null || key.IsUsed)
            return false;

        // Check if key is expired
        if (key.ExpiresAt.HasValue && key.ExpiresAt.Value < DateTime.UtcNow)
            return false;

        return true;
    }

    public async Task AddAsync(AdminKey adminKey)
    {
        await _context.AdminKeys.AddAsync(adminKey);
    }

    public async Task UpdateAsync(AdminKey adminKey)
    {
        _context.AdminKeys.Update(adminKey);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
