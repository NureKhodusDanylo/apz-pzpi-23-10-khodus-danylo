using Application.DTOs.UserDTOs;
using Application.DTOs.NodeDTOs;
using Application.DTOs.FileDTOs;

namespace Application.Abstractions.Interfaces
{
    public interface IUserService
    {
        Task<UserProfileDTO?> GetUserByIdAsync(int userId);
        Task<IEnumerable<UserProfileDTO>> GetAllUsersAsync();
        Task<IEnumerable<UserSearchResultDTO>> SearchUsersAsync(string query, int currentUserId);
        Task<UserProfileDTO?> GetProfileAsync(int userId);
        Task<NodeResponseDTO?> GetMyNodeAsync(int userId);
        Task<NodeResponseDTO> UpdateMyNodeAsync(int userId, UpdateMyNodeDTO updateDto);
        Task<UserProfileDTO> UpdateProfileAsync(int userId, UpdateUserProfileDTO updateDto);
        Task<UserProfileDTO> UpdateProfileWithPhotoAsync(int userId, UpdateUserProfileDTO updateDto, FileUploadDTO? profilePhoto, string contentRootPath);
        Task<FileResponseDTO?> GetProfilePhotoAsync(int userId);
        Task<byte[]?> GetProfilePhotoContentAsync(int userId, string contentRootPath);
    }
}
