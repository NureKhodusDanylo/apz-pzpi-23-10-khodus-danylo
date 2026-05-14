using Application.Abstractions.Interfaces;
using Application.DTOs.UserDTOs;
using Entities;
using Entities.Config;
using Entities.Interfaces;
using Infrastructure.Repository;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;


namespace Application.Services
{

    public class BaseTokenService : ITokenService
    {
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _tokenExpiresHours;
        private readonly IUserRepository _userRepository;
        private readonly IGoogleTokenValidator _googleTokenValidator;
        private readonly IRobotRepository _robotRepository;
        //private readonly Config _config;

        public BaseTokenService(Config config, IUserRepository userRepository, IGoogleTokenValidator googleTokenValidator, IRobotRepository robotRepository)
        {
            //_config = config;
            _secretKey = config.Jwt.Key;
            _issuer = config.Jwt.Issuer;
            _audience = config.Jwt.Audience;
            _tokenExpiresHours = config.Jwt.TokenExpiresHours;
            _userRepository = userRepository;
            _googleTokenValidator = googleTokenValidator;
            _robotRepository = robotRepository;
        }


        public async Task<string?> GenerateToken(UserLoginDTO loginData)
        {
            List<Claim> claims = new();
            int? UserId = null;
            Entities.Models.User? user = null;

            if (loginData.Email != null)
            {
                user = await _userRepository.GetByEmailAsync(loginData.Email);
                UserId = user?.Id;
            }
            else if (loginData.googleJwtToken != null)
            {
                string Id = (await _googleTokenValidator.ValidateAsync(loginData.googleJwtToken)).Subject;
                user = await _userRepository.GetByGoogleIdAsync(Id);
                UserId = user?.Id;
            }
            else
                return null;


            if (UserId == null)
            {
                throw new UnauthorizedAccessException("User not found");
            }

            claims.Add(new Claim("Id", UserId.Value.ToString()));

            // Add role claim if user has a role
            if (user?.Role != null)
            {
                claims.Add(new Claim(ClaimTypes.Role, user.Role.Value.ToString()));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(_tokenExpiresHours),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        public int GetUserIdFromToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secretKey);

            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = _issuer,
                ValidAudience = _audience,
                ValidateLifetime = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuerSigningKey = true
            }, out SecurityToken validatedToken);

            var userIdClaim = principal.Claims.FirstOrDefault(c => c.Type == "Id");

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                throw new SecurityTokenException("Id claim missing or invalid");
            }

            return userId;
        }

        public async Task<string?> GenerateTokenByUserId(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new UnauthorizedAccessException("User not found");
            }

            List<Claim> claims = new()
            {
                new Claim("Id", userId.ToString())
            };

            if (user.Role != null)
            {
                claims.Add(new Claim(ClaimTypes.Role, user.Role.Value.ToString()));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(_tokenExpiresHours),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<string?> GenerateRobotToken(int robotId)
        {
            var robot = await _robotRepository.GetByIdAsync(robotId);

            if (robot == null)
            {
                throw new UnauthorizedAccessException("Robot not found");
            }

            List<Claim> claims = new()
            {
                new Claim("Id", robotId.ToString()),
                new Claim("RobotId", robotId.ToString()),
                new Claim(ClaimTypes.Role, "Iot"),
                new Claim("Type", "Robot")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(_tokenExpiresHours * 24), // Longer expiration for IoT devices (24 days)
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}
