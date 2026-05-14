using Application.Abstractions.Interfaces;
using Application.DTOs.NodeDTOs;
using Entities.Interfaces;
using Entities.Models;

namespace Application.Services
{
    public class NodeService : INodeService
    {
        private readonly INodeRepository _nodeRepository;

        public NodeService(INodeRepository nodeRepository)
        {
            _nodeRepository = nodeRepository;
        }

        public async Task<NodeResponseDTO> CreateNodeAsync(CreateNodeDTO nodeDto)
        {
            // Validate data
            if (string.IsNullOrWhiteSpace(nodeDto.Name))
            {
                throw new ArgumentException("Node name is required");
            }

            if (nodeDto.Latitude < -90 || nodeDto.Latitude > 90)
            {
                throw new ArgumentException("Latitude must be between -90 and 90");
            }

            if (nodeDto.Longitude < -180 || nodeDto.Longitude > 180)
            {
                throw new ArgumentException("Longitude must be between -180 and 180");
            }

            var node = new Node
            {
                Name = nodeDto.Name,
                Latitude = nodeDto.Latitude,
                Longitude = nodeDto.Longitude,
                Type = nodeDto.Type
            };

            await _nodeRepository.AddAsync(node);

            return MapToResponseDTO(node);
        }

        public async Task<NodeResponseDTO?> GetNodeByIdAsync(int nodeId)
        {
            var node = await _nodeRepository.GetByIdAsync(nodeId);
            return node == null ? null : MapToResponseDTO(node);
        }

        public async Task<IEnumerable<NodeResponseDTO>> GetAllNodesAsync()
        {
            var nodes = await _nodeRepository.GetAllAsync();
            return nodes.Select(MapToResponseDTO);
        }

        public async Task<IEnumerable<NodeResponseDTO>> GetNodesByTypeAsync(NodeType type)
        {
            var nodes = await _nodeRepository.GetByTypeAsync(type);
            return nodes.Select(MapToResponseDTO);
        }

        public async Task<NodeResponseDTO?> FindNearestNodeAsync(double latitude, double longitude, NodeType? type = null)
        {
            // Validate coordinates
            if (latitude < -90 || latitude > 90)
            {
                throw new ArgumentException("Latitude must be between -90 and 90");
            }

            if (longitude < -180 || longitude > 180)
            {
                throw new ArgumentException("Longitude must be between -180 and 180");
            }

            var node = await _nodeRepository.FindNearestNodeAsync(latitude, longitude, type);
            return node == null ? null : MapToResponseDTO(node);
        }

        public async Task<bool> UpdateNodeAsync(UpdateNodeDTO nodeDto)
        {
            var existingNode = await _nodeRepository.GetByIdAsync(nodeDto.Id);
            if (existingNode == null)
            {
                return false;
            }

            // Validate data
            if (string.IsNullOrWhiteSpace(nodeDto.Name))
            {
                throw new ArgumentException("Node name is required");
            }

            if (nodeDto.Latitude < -90 || nodeDto.Latitude > 90)
            {
                throw new ArgumentException("Latitude must be between -90 and 90");
            }

            if (nodeDto.Longitude < -180 || nodeDto.Longitude > 180)
            {
                throw new ArgumentException("Longitude must be between -180 and 180");
            }

            existingNode.Name = nodeDto.Name;
            existingNode.Latitude = nodeDto.Latitude;
            existingNode.Longitude = nodeDto.Longitude;
            existingNode.Type = nodeDto.Type;

            await _nodeRepository.UpdateAsync(existingNode);
            return true;
        }

        public async Task<bool> DeleteNodeAsync(int nodeId)
        {
            if (!await _nodeRepository.ExistsAsync(nodeId))
            {
                return false;
            }

            await _nodeRepository.DeleteAsync(nodeId);
            return true;
        }

        private static NodeResponseDTO MapToResponseDTO(Node node)
        {
            return new NodeResponseDTO
            {
                Id = node.Id,
                Name = node.Name,
                Latitude = node.Latitude,
                Longitude = node.Longitude,
                Type = node.Type,
                TypeName = node.Type.ToString()
            };
        }
    }
}
