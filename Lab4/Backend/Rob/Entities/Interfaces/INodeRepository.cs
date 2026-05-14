using Entities.Models;

namespace Entities.Interfaces
{
    public interface INodeRepository
    {
        Task AddAsync(Node node);
        Task<Node?> GetByIdAsync(int nodeId);
        Task<IEnumerable<Node>> GetAllAsync();
        Task<IEnumerable<Node>> GetByTypeAsync(NodeType type);
        Task<Node?> FindNearestNodeAsync(double latitude, double longitude, NodeType? type = null, IEnumerable<int>? excludeIds = null);
        Task UpdateAsync(Node node);
        Task DeleteAsync(int nodeId);
        Task<bool> ExistsAsync(int nodeId);
        Task SaveChangesAsync();
    }
}
