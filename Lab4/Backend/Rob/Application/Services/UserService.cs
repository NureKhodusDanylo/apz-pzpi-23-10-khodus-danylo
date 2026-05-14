using Application.Abstractions.Interfaces;
using Application.DTOs.UserDTOs;
using Application.DTOs.NodeDTOs;
using Application.DTOs.FileDTOs;
using Entities.Interfaces;

namespace Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly INodeRepository _nodeRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IFileService _fileService;

        public UserService(
            IUserRepository userRepository,
            INodeRepository nodeRepository,
            IPasswordHasher passwordHasher,
            IFileService fileService)
        {
            _userRepository = userRepository;
            _nodeRepository = nodeRepository;
            _passwordHasher = passwordHasher;
            _fileService = fileService;
        }

        private string? GetProfilePhotoUrl(int userId, bool hasPhoto)
        {
            if (!hasPhoto)
                return null;

            // Return endpoint URL that can be used to fetch the photo
            // Frontend should prepend the base URL (e.g., http://localhost:5102)
            return $"/api/User/{userId}/photo";
        }

        public async Task<UserProfileDTO?> GetUserByIdAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return null;
            }

            return new UserProfileDTO
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                GoogleId = user.GoogleId,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role?.ToString(),
                SentOrdersCount = user.SentOrders?.Count ?? 0,
                ReceivedOrdersCount = user.ReceivedOrders?.Count ?? 0,
                ProfilePhotoUrl = GetProfilePhotoUrl(user.Id, user.ProfilePhotoId.HasValue),
                PersonalNodeId = user.PersonalNodeId,
                Address = user.PersonalNode?.Name,
                Latitude = user.PersonalNode?.Latitude,
                Longitude = user.PersonalNode?.Longitude,
                Balance = user.Balance
            };
        }

        public async Task<IEnumerable<UserProfileDTO>> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllAsync();

            return users.Select(user => new UserProfileDTO
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                GoogleId = user.GoogleId,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role?.ToString(),
                SentOrdersCount = user.SentOrders?.Count ?? 0,
                ReceivedOrdersCount = user.ReceivedOrders?.Count ?? 0,
                ProfilePhotoUrl = GetProfilePhotoUrl(user.Id, user.ProfilePhotoId.HasValue),
                PersonalNodeId = user.PersonalNodeId,
                Address = user.PersonalNode?.Name,
                Latitude = user.PersonalNode?.Latitude,
                Longitude = user.PersonalNode?.Longitude,
                Balance = user.Balance
            }).ToList();
        }

        public async Task<IEnumerable<UserSearchResultDTO>> SearchUsersAsync(string query, int currentUserId)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentException("Search query cannot be empty");
            }

            var users = await _userRepository.SearchUsersAsync(query);

            // Exclude the current user from search results
            return users
                .Where(user => user.Id != currentUserId)
                .Select(user => new UserSearchResultDTO
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    PhoneNumber = user.PhoneNumber,
                    Address = user.PersonalNode?.Name
                }).ToList();
        }

        public async Task<UserProfileDTO?> GetProfileAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return null;
            }

            return new UserProfileDTO
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                GoogleId = user.GoogleId,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role?.ToString(),
                SentOrdersCount = user.SentOrders?.Count ?? 0,
                ReceivedOrdersCount = user.ReceivedOrders?.Count ?? 0,
                ProfilePhotoUrl = GetProfilePhotoUrl(user.Id, user.ProfilePhotoId.HasValue),
                PersonalNodeId = user.PersonalNodeId,
                Address = user.PersonalNode?.Name,
                Latitude = user.PersonalNode?.Latitude,
                Longitude = user.PersonalNode?.Longitude,
                Balance = user.Balance
            };
        }

        public async Task<NodeResponseDTO?> GetMyNodeAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new ArgumentException("User not found");
            }

            if (user.PersonalNodeId == null)
            {
                return null;
            }

            var node = await _nodeRepository.GetByIdAsync(user.PersonalNodeId.Value);
            if (node == null)
            {
                return null;
            }

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

        public async Task<NodeResponseDTO> UpdateMyNodeAsync(int userId, UpdateMyNodeDTO updateDto)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new ArgumentException("User not found");
            }

            if (user.PersonalNodeId == null)
            {
                throw new ArgumentException("Personal node not found");
            }

            var node = await _nodeRepository.GetByIdAsync(user.PersonalNodeId.Value);
            if (node == null)
            {
                throw new ArgumentException("Personal node not found");
            }

            // Update node coordinates and address
            node.Latitude = updateDto.Latitude;
            node.Longitude = updateDto.Longitude;
            if (!string.IsNullOrEmpty(updateDto.Address))
            {
                node.Name = updateDto.Address;
            }

            await _nodeRepository.UpdateAsync(node);
            await _nodeRepository.SaveChangesAsync();

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

        public async Task<UserProfileDTO> UpdateProfileAsync(int userId, UpdateUserProfileDTO updateDto)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new ArgumentException("User not found");
            }

            // Update username if provided
            if (!string.IsNullOrWhiteSpace(updateDto.UserName))
            {
                user.UserName = updateDto.UserName;
            }

            // Update phone number if provided
            if (!string.IsNullOrWhiteSpace(updateDto.PhoneNumber))
            {
                user.PhoneNumber = updateDto.PhoneNumber;
            }

            // Update password if provided
            if (!string.IsNullOrWhiteSpace(updateDto.Password))
            {
                user.PasswordHash = _passwordHasher.Hash(updateDto.Password);
            }

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            return new UserProfileDTO
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                GoogleId = user.GoogleId,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role?.ToString(),
                SentOrdersCount = user.SentOrders?.Count ?? 0,
                ReceivedOrdersCount = user.ReceivedOrders?.Count ?? 0,
                ProfilePhotoUrl = GetProfilePhotoUrl(user.Id, user.ProfilePhotoId.HasValue),
                PersonalNodeId = user.PersonalNodeId,
                Address = user.PersonalNode?.Name,
                Latitude = user.PersonalNode?.Latitude,
                Longitude = user.PersonalNode?.Longitude,
                Balance = user.Balance
            };
        }

        public async Task<UserProfileDTO> UpdateProfileWithPhotoAsync(int userId, UpdateUserProfileDTO updateDto, FileUploadDTO? profilePhoto, string contentRootPath)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new ArgumentException("User not found");
            }

            // Update basic profile info
            if (!string.IsNullOrWhiteSpace(updateDto.UserName))
            {
                user.UserName = updateDto.UserName;
            }

            if (!string.IsNullOrWhiteSpace(updateDto.PhoneNumber))
            {
                user.PhoneNumber = updateDto.PhoneNumber;
            }

            if (!string.IsNullOrWhiteSpace(updateDto.Password))
            {
                user.PasswordHash = _passwordHasher.Hash(updateDto.Password);
            }

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            // Handle profile photo upload if provided
            if (profilePhoto != null)
            {
                var photoDto = await _fileService.UploadProfilePhotoAsync(userId, profilePhoto, contentRootPath);
                // Photo URL will be updated after successful upload
            }

            // Reload user to get updated photo reference
            user = await _userRepository.GetByIdAsync(userId);

            return new UserProfileDTO
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                GoogleId = user.GoogleId,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role?.ToString(),
                SentOrdersCount = user.SentOrders?.Count ?? 0,
                ReceivedOrdersCount = user.ReceivedOrders?.Count ?? 0,
                ProfilePhotoUrl = GetProfilePhotoUrl(user.Id, user.ProfilePhotoId.HasValue),
                PersonalNodeId = user.PersonalNodeId,
                Address = user.PersonalNode?.Name,
                Latitude = user.PersonalNode?.Latitude,
                Longitude = user.PersonalNode?.Longitude,
                Balance = user.Balance
            };
        }

        public async Task<FileResponseDTO?> GetProfilePhotoAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || !user.ProfilePhotoId.HasValue)
            {
                return null;
            }

            return await _fileService.GetFileByIdAsync(user.ProfilePhotoId.Value);
        }

        public async Task<byte[]?> GetProfilePhotoContentAsync(int userId, string contentRootPath)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || !user.ProfilePhotoId.HasValue)
            {
                return null;
            }

            return await _fileService.GetFileContentAsync(user.ProfilePhotoId.Value, contentRootPath);
        }
    }
}
