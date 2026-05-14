using Entities.Interfaces;
using Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repository
{
    public class NodeRepository : GenericRepository<Node>, INodeRepository
    {
        private readonly MyDbContext _context;

        public NodeRepository(MyDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Node?> GetByIdAsync(int nodeId)
        {
            return await _context.Nodes.FindAsync(nodeId);
        }

        public async Task<IEnumerable<Node>> GetAllAsync()
        {
            return await _context.Nodes
                .OrderBy(n => n.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Node>> GetByTypeAsync(NodeType type)
        {
            return await _context.Nodes
                .Where(n => n.Type == type)
                .OrderBy(n => n.Name)
                .ToListAsync();
        }

        public async Task<Node?> FindNearestNodeAsync(double latitude, double longitude, NodeType? type = null, IEnumerable<int>? excludeIds = null)
        {
            var query = _context.Nodes.AsQueryable();

            if (type.HasValue)
            {
                query = query.Where(n => n.Type == type.Value);
            }

            if (excludeIds != null && excludeIds.Any())
            {
                query = query.Where(n => !excludeIds.Contains(n.Id));
            }

            // Calculate distance using Haversine formula approximation
            // For simplicity, using Pythagorean theorem (works for short distances)
            var nodes = await query.ToListAsync();

            return nodes
                .OrderBy(n => Math.Pow(n.Latitude - latitude, 2) + Math.Pow(n.Longitude - longitude, 2))
                .FirstOrDefault();
        }

        public async Task UpdateAsync(Node node)
        {
            _context.Nodes.Update(node);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int nodeId)
        {
            var node = await _context.Nodes.FindAsync(nodeId);
            if (node != null)
            {
                _context.Nodes.Remove(node);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int nodeId)
        {
            return await _context.Nodes.AnyAsync(n => n.Id == nodeId);
        }
    }
}
