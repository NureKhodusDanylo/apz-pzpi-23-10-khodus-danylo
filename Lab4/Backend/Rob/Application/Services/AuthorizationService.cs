using Application.Abstractions.Interfaces;
using Application.DTOs.UserDTOs;
using Entities;
using Entities.Enums;
using Entities.Interfaces;
using Entities.Models;
using Google.Apis.Auth;

namespace Application.Services
{
    public class AuthorizationService : IAuthorizationService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IGoogleTokenValidator _googleTokenValidator;
        private readonly INodeRepository _nodeRepository;
        private readonly IAdminKeyRepository _adminKeyRepository;
        private readonly ITokenService _tokenService;

        public AuthorizationService(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IGoogleTokenValidator googleTokenValidator,
            INodeRepository nodeRepository,
            IAdminKeyRepository adminKeyRepository,
            ITokenService tokenService)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _googleTokenValidator = googleTokenValidator;
            _nodeRepository = nodeRepository;
            _adminKeyRepository = adminKeyRepository;
            _tokenService = tokenService;
        }

        /// <summary>
        /// Unified register endpoint:
        /// - Password: if user exists → auto-login; if not → create + auto-login
        /// - Google: if user exists → auto-login; if not → return NeedsAdditionalInfo
        /// </summary>
        public async Task<AuthResultDTO> RegisterOrLoginAsync(UserRegisterDTO registerData)
        {
            if (!string.IsNullOrEmpty(registerData.googleJwtToken))
            {
                return await RegisterOrLoginWithGoogleAsync(registerData);
            }

            if (!string.IsNullOrEmpty(registerData.Password))
            {
                return await RegisterOrLoginWithPasswordAsync(registerData);
            }

            return new AuthResultDTO
            {
                Status = "Error",
                Message = "No authentication method provided"
            };
        }

        /// <summary>
        /// Unified login endpoint:
        /// - Password: normal login
        /// - Google: if user exists → login; if not → return NeedsAdditionalInfo
        /// </summary>
        public async Task<AuthResultDTO> LoginOrRegisterAsync(UserLoginDTO loginData)
        {
            if (!string.IsNullOrEmpty(loginData.googleJwtToken))
            {
                return await LoginOrRegisterWithGoogleAsync(loginData.googleJwtToken);
            }

            if (!string.IsNullOrEmpty(loginData.Password))
            {
                return await LoginWithPasswordAsync(loginData.Email, loginData.Password);
            }

            return new AuthResultDTO
            {
                Status = "Error",
                Message = "No authentication method provided"
            };
        }

        /// <summary>
        /// Completes Google registration with additional info (phone, location)
        /// </summary>
        public async Task<AuthResultDTO> CompleteGoogleRegistrationAsync(CompleteGoogleRegDTO data)
        {
            // Check if user already registered (race condition guard)
            var existingUser = await _userRepository.GetByGoogleIdAsync(data.GoogleId);
            if (existingUser != null)
            {
                var existingToken = await _tokenService.GenerateTokenByUserId(existingUser.Id);
                return new AuthResultDTO
                {
                    Status = "Success",
                    Token = existingToken,
                    Message = "Account already exists, logged in"
                };
            }

            // Create personal node
            var personalNode = new Node
            {
                Name = string.IsNullOrEmpty(data.Address)
                    ? $"{data.UserName}'s Location"
                    : data.Address,
                Latitude = data.Latitude,
                Longitude = data.Longitude,
                Type = NodeType.UserNode
            };

            await _nodeRepository.AddAsync(personalNode);
            await _nodeRepository.SaveChangesAsync();

            // Determine role
            UserRole userRole = await DetermineUserRoleAsync(data.AdminKey);

            var user = new User
            {
                UserName = data.UserName,
                Email = data.Email,
                GoogleId = data.GoogleId,
                PhoneNumber = data.PhoneNumber,
                PasswordHash = string.Empty, // Google users don't have a password
                Role = userRole,
                PersonalNodeId = personalNode.Id
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            // Mark admin key as used
            await TryMarkAdminKeyUsedAsync(data.AdminKey, userRole, user.Id);

            // Generate token
            var token = await _tokenService.GenerateTokenByUserId(user.Id);

            return new AuthResultDTO
            {
                Status = "Success",
                Token = token,
                Message = "Registration completed successfully"
            };
        }

        // ─── Private helpers ────────────────────────────────────────────────

        private async Task<AuthResultDTO> RegisterOrLoginWithPasswordAsync(UserRegisterDTO registerData)
        {
            // If user exists → auto-login
            if (await _userRepository.ExistsByEmailAsync(registerData.Email))
            {
                string passwordHash = _passwordHasher.Hash(registerData.Password);
                bool isValid = await _userRepository.IsPasswordValidByEmailAsync(registerData.Email, passwordHash);

                if (!isValid)
                {
                    return new AuthResultDTO
                    {
                        Status = "Error",
                        Message = "Account exists but password is incorrect"
                    };
                }

                var existingUser = await _userRepository.GetByEmailAsync(registerData.Email);
                var existingToken = await _tokenService.GenerateTokenByUserId(existingUser!.Id);

                return new AuthResultDTO
                {
                    Status = "Success",
                    Token = existingToken,
                    Message = "Account already exists, logged in"
                };
            }

            // Create new user
            var personalNode = new Node
            {
                Name = string.IsNullOrEmpty(registerData.Address)
                    ? $"{registerData.UserName}'s Location"
                    : registerData.Address,
                Latitude = registerData.Latitude,
                Longitude = registerData.Longitude,
                Type = NodeType.UserNode
            };

            await _nodeRepository.AddAsync(personalNode);
            await _nodeRepository.SaveChangesAsync();

            string hash = _passwordHasher.Hash(registerData.Password);
            UserRole userRole = await DetermineUserRoleAsync(registerData.AdminKey);

            var user = new User
            {
                UserName = registerData.UserName,
                Email = registerData.Email,
                PasswordHash = hash,
                PhoneNumber = registerData.PhoneNumber,
                Role = userRole,
                PersonalNodeId = personalNode.Id
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            await TryMarkAdminKeyUsedAsync(registerData.AdminKey, userRole, user.Id);

            var token = await _tokenService.GenerateTokenByUserId(user.Id);

            return new AuthResultDTO
            {
                Status = "Success",
                Token = token,
                Message = "Registered and logged in successfully"
            };
        }

        private async Task<AuthResultDTO> RegisterOrLoginWithGoogleAsync(UserRegisterDTO registerData)
        {
            try
            {
                var payload = await _googleTokenValidator.ValidateAsync(registerData.googleJwtToken);

                // If user already exists → auto-login
                var existingUser = await _userRepository.GetByGoogleIdAsync(payload.Subject);
                if (existingUser != null)
                {
                    var existingToken = await _tokenService.GenerateTokenByUserId(existingUser.Id);
                    return new AuthResultDTO
                    {
                        Status = "Success",
                        Token = existingToken,
                        Message = "Account already exists, logged in"
                    };
                }

                // User doesn't exist → need additional info (phone, address)
                return new AuthResultDTO
                {
                    Status = "NeedsAdditionalInfo",
                    Message = "Please provide additional information to complete registration",
                    GoogleId = payload.Subject,
                    Email = payload.Email,
                    UserName = payload.Name
                };
            }
            catch (InvalidJwtException)
            {
                return new AuthResultDTO
                {
                    Status = "Error",
                    Message = "Invalid Google token"
                };
            }
        }

        private async Task<AuthResultDTO> LoginWithPasswordAsync(string email, string password)
        {
            if (!await _userRepository.ExistsByEmailAsync(email))
            {
                return new AuthResultDTO
                {
                    Status = "Error",
                    Message = "Incorrect email"
                };
            }

            string passwordHash = _passwordHasher.Hash(password);
            bool isValid = await _userRepository.IsPasswordValidByEmailAsync(email, passwordHash);

            if (!isValid)
            {
                return new AuthResultDTO
                {
                    Status = "Error",
                    Message = "Incorrect password"
                };
            }

            var user = await _userRepository.GetByEmailAsync(email);
            var token = await _tokenService.GenerateTokenByUserId(user!.Id);

            return new AuthResultDTO
            {
                Status = "Success",
                Token = token,
                Message = "Login successful"
            };
        }

        private async Task<AuthResultDTO> LoginOrRegisterWithGoogleAsync(string googleJwtToken)
        {
            try
            {
                var payload = await _googleTokenValidator.ValidateAsync(googleJwtToken);

                var existingUser = await _userRepository.GetByGoogleIdAsync(payload.Subject);
                if (existingUser != null)
                {
                    var token = await _tokenService.GenerateTokenByUserId(existingUser.Id);
                    return new AuthResultDTO
                    {
                        Status = "Success",
                        Token = token,
                        Message = "Login successful"
                    };
                }

                // User doesn't exist → need additional info
                return new AuthResultDTO
                {
                    Status = "NeedsAdditionalInfo",
                    Message = "Please provide additional information to complete registration",
                    GoogleId = payload.Subject,
                    Email = payload.Email,
                    UserName = payload.Name
                };
            }
            catch (InvalidJwtException)
            {
                return new AuthResultDTO
                {
                    Status = "Error",
                    Message = "Invalid Google token"
                };
            }
        }

        private async Task<UserRole> DetermineUserRoleAsync(string? adminKey)
        {
            var allUsers = await _userRepository.GetAllAsync();
            var isFirstUser = !allUsers.Any();

            if (!string.IsNullOrEmpty(adminKey))
            {
                var isValidKey = await _adminKeyRepository.IsKeyValidAsync(adminKey);
                if (isValidKey)
                {
                    return UserRole.Admin;
                }
            }

            if (isFirstUser)
            {
                return UserRole.Admin;
            }

            return UserRole.User;
        }

        private async Task TryMarkAdminKeyUsedAsync(string? adminKey, UserRole role, int userId)
        {
            if (!string.IsNullOrEmpty(adminKey) && role == UserRole.Admin)
            {
                var key = await _adminKeyRepository.GetByKeyCodeAsync(adminKey);
                if (key != null)
                {
                    key.IsUsed = true;
                    key.UsedAt = DateTime.UtcNow;
                    key.UsedByUserId = userId;

                    await _adminKeyRepository.UpdateAsync(key);
                    await _adminKeyRepository.SaveChangesAsync();
                }
            }
        }
    }
}