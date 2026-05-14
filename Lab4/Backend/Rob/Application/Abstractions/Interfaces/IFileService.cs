using Application.DTOs.FileDTOs;

namespace Application.Abstractions.Interfaces
{
    public interface IFileService
    {
        Task<FileResponseDTO> UploadProfilePhotoAsync(int userId, FileUploadDTO file, string contentRootPath);
        Task<List<FileResponseDTO>> UploadOrderImagesAsync(int orderId, List<FileUploadDTO> files, string contentRootPath);
        Task<FileResponseDTO?> GetFileByIdAsync(int fileId);
        Task<List<FileResponseDTO>> GetOrderFilesAsync(int orderId);
        Task<bool> DeleteFileAsync(int fileId, string contentRootPath);
        Task<bool> DeleteProfilePhotoAsync(int userId, string contentRootPath);
        Task<byte[]?> GetFileContentAsync(int fileId, string contentRootPath);
        Task<bool> IsUserAuthorizedForFileAsync(int fileId, int userId, bool isAdmin);
    }
}
