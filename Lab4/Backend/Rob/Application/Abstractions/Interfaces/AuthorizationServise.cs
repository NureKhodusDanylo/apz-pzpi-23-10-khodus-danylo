using Application.DTOs.UserDTOs;

namespace Application.Abstractions.Interfaces
{
    public interface IAuthorizationService
    {
        Task<AuthResultDTO> RegisterOrLoginAsync(UserRegisterDTO registerData);
        Task<AuthResultDTO> LoginOrRegisterAsync(UserLoginDTO loginData);
        Task<AuthResultDTO> CompleteGoogleRegistrationAsync(CompleteGoogleRegDTO data);
    }
}