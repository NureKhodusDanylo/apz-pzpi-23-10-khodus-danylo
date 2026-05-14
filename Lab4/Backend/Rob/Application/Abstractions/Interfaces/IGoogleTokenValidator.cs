using Google.Apis.Auth;
using System.Threading.Tasks;

namespace Application.Abstractions.Interfaces
{
    public interface IGoogleTokenValidator
    {
        Task<GoogleJsonWebSignature.Payload> ValidateAsync(string jwtToken);
    }
}