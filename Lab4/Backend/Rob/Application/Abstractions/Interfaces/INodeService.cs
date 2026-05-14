using Application.DTOs.NodeDTOs;

namespace Application.Abstractions.Interfaces
{
    public interface INodeService
    {
        Task<NodeResponseDTO> CreateNodeAsync(CreateNodeDTO nodeDto);
        Task<NodeResponseDTO?> GetNodeByIdAsync(int nodeId);
        Task<IEnumerable<NodeResponseDTO>> GetAllNodesAsync();
        Task<IEnumerable<NodeResponseDTO>> GetNodesByTypeAsync(NodeType type);
        Task<NodeResponseDTO?> FindNearestNodeAsync(double latitude, double longitude, NodeType? type = null);
        Task<bool> UpdateNodeAsync(UpdateNodeDTO nodeDto);
        Task<bool> DeleteNodeAsync(int nodeId);
    }
}
