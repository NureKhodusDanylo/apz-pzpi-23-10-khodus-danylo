using Application.DTOs.MapDTOs;

namespace Application.Abstractions.Interfaces
{
    public interface IMapService
    {
        Task<MapDataResponseDTO> GetMapDataAsync(int? userId = null);
    }
}
