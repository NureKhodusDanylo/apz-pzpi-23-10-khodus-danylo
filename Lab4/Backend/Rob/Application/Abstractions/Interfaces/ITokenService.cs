using Application.DTOs.UserDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Interfaces
{
    public interface ITokenService
    {
        Task<string?> GenerateToken(UserLoginDTO loginData);
        Task<string?> GenerateTokenByUserId(int userId);
        Task<string?> GenerateRobotToken(int robotId);
    }
}
