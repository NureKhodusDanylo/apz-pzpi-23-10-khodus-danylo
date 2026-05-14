using Application.Abstractions.Interfaces;
using Application.DTOs.FileDTOs;
using Entities.Interfaces;
using FileEntity = Entities.Models.File;

namespace Application.Services
{
    public class FileService : IFileService
    {
        private readonly IFileRepository _fileRepository;
        private readonly IUserRepository _userRepository;
        private readonly IOrderRepository _orderRepository;

        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
        private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

        public FileService(
            IFileRepository fileRepository,
            IUserRepository userRepository,
            IOrderRepository orderRepository)
        {
            _fileRepository = fileRepository;
            _userRepository = userRepository;
            _orderRepository = orderRepository;
        }

        public async Task<FileResponseDTO> UploadProfilePhotoAsync(int userId, FileUploadDTO file, string contentRootPath)
        {
            // Validate user exists
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new ArgumentException("User not found");
            }

            // Validate file
            ValidateFile(file);

            // Delete old profile photo if exists
            if (user.ProfilePhotoId.HasValue)
            {
                await DeleteProfilePhotoAsync(userId, contentRootPath);
            }

            // Create upload directory
            var uploadsPath = Path.Combine(contentRootPath, "Uploads", "Profiles");
            Directory.CreateDirectory(uploadsPath);

            // Generate unique filename
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var uniqueFileName = $"{userId}_{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsPath, uniqueFileName);

            // Save file to disk
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.Content.CopyToAsync(fileStream);
            }

            // Create file entity
            var fileEntity = new FileEntity
            {
                FileName = file.FileName,
                FilePath = $"/Uploads/Profiles/{uniqueFileName}",
                ContentType = file.ContentType,
                FileSize = file.Length,
                UserId = userId,
                UploadedAt = DateTime.UtcNow
            };

            await _fileRepository.AddAsync(fileEntity);
            await _fileRepository.SaveChangesAsync();

            // Update user's profile photo reference
            user.ProfilePhotoId = fileEntity.Id;
            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            return MapToResponseDTO(fileEntity);
        }

        public async Task<List<FileResponseDTO>> UploadOrderImagesAsync(int orderId, List<FileUploadDTO> files, string contentRootPath)
        {
            // Validate order exists
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
            {
                throw new ArgumentException("Order not found");
            }

            if (files == null || files.Count == 0)
            {
                throw new ArgumentException("No files provided");
            }

            var uploadedFiles = new List<FileResponseDTO>();

            // Create upload directory
            var uploadsPath = Path.Combine(contentRootPath, "Uploads", "Orders", orderId.ToString());
            Directory.CreateDirectory(uploadsPath);

            foreach (var file in files)
            {
                // Validate file
                ValidateFile(file);

                // Generate unique filename
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsPath, uniqueFileName);

                // Save file to disk
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.Content.CopyToAsync(fileStream);
                }

                // Create file entity
                var fileEntity = new FileEntity
                {
                    FileName = file.FileName,
                    FilePath = $"/Uploads/Orders/{orderId}/{uniqueFileName}",
                    ContentType = file.ContentType,
                    FileSize = file.Length,
                    OrderId = orderId,
                    UploadedAt = DateTime.UtcNow
                };

                await _fileRepository.AddAsync(fileEntity);
                uploadedFiles.Add(MapToResponseDTO(fileEntity));
            }

            await _fileRepository.SaveChangesAsync();

            return uploadedFiles;
        }

        public async Task<FileResponseDTO?> GetFileByIdAsync(int fileId)
        {
            var file = await _fileRepository.GetByIdAsync(fileId);
            return file == null ? null : MapToResponseDTO(file);
        }

        public async Task<List<FileResponseDTO>> GetOrderFilesAsync(int orderId)
        {
            var files = await _fileRepository.GetByOrderIdAsync(orderId);
            return files.Select(MapToResponseDTO).ToList();
        }

        public async Task<bool> DeleteFileAsync(int fileId, string contentRootPath)
        {
            var file = await _fileRepository.GetByIdAsync(fileId);
            if (file == null)
            {
                return false;
            }

            // Delete file from disk
            var diskPath = Path.Combine(contentRootPath, file.FilePath.TrimStart('/'));
            if (File.Exists(diskPath))
            {
                File.Delete(diskPath);
            }

            // Delete from database
            await _fileRepository.DeleteAsync(fileId);
            await _fileRepository.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteProfilePhotoAsync(int userId, string contentRootPath)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || !user.ProfilePhotoId.HasValue)
            {
                return false;
            }

            var photo = await _fileRepository.GetByIdAsync(user.ProfilePhotoId.Value);
            if (photo != null)
            {
                // Delete file from disk
                var oldFilePath = Path.Combine(contentRootPath, photo.FilePath.TrimStart('/'));
                if (File.Exists(oldFilePath))
                {
                    File.Delete(oldFilePath);
                }

                await _fileRepository.DeleteAsync(photo.Id);
                await _fileRepository.SaveChangesAsync();
            }

            return true;
        }

        public async Task<byte[]?> GetFileContentAsync(int fileId, string contentRootPath)
        {
            var file = await _fileRepository.GetByIdAsync(fileId);
            if (file == null)
            {
                return null;
            }

            var filePath = Path.Combine(contentRootPath, file.FilePath.TrimStart('/'));
            if (!File.Exists(filePath))
            {
                return null;
            }

            return await File.ReadAllBytesAsync(filePath);
        }

        public async Task<bool> IsUserAuthorizedForFileAsync(int fileId, int userId, bool isAdmin)
        {
            var file = await _fileRepository.GetByIdAsync(fileId);
            if (file == null)
            {
                return false;
            }

            // Admin has access to all files
            if (isAdmin)
            {
                return true;
            }

            // Check if user owns this file (either through order or profile photo)
            if (file.OrderId.HasValue)
            {
                var order = await _orderRepository.GetByIdAsync(file.OrderId.Value);
                if (order != null && order.SenderId == userId)
                {
                    return true;
                }
            }
            else if (file.UserId.HasValue && file.UserId == userId)
            {
                return true;
            }

            return false;
        }

        private void ValidateFile(FileUploadDTO file)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!_allowedExtensions.Contains(extension))
            {
                throw new ArgumentException($"Invalid file extension. Allowed: {string.Join(", ", _allowedExtensions)}");
            }

            if (file.Length > MaxFileSize)
            {
                throw new ArgumentException("File exceeds maximum size of 5MB");
            }
        }

        private static FileResponseDTO MapToResponseDTO(FileEntity file)
        {
            return new FileResponseDTO
            {
                Id = file.Id,
                FileName = file.FileName,
                FilePath = file.FilePath,
                ContentType = file.ContentType,
                FileSize = file.FileSize,
                UploadedAt = file.UploadedAt
            };
        }
    }
}
