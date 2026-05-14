using Application.Abstractions.Interfaces;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace Application.Services
{
    public class GoogleTokenValidator : IGoogleTokenValidator
    {
        private readonly string _googleClientId;
        private readonly string _mobileClientId;

        public GoogleTokenValidator(IConfiguration configuration)
        {
            _googleClientId = configuration["Authentication:Google:ClientId"] ?? string.Empty;
            _mobileClientId = configuration["Authentication:Google:MobileClientId"] ?? string.Empty;
        }

        public async Task<GoogleJsonWebSignature.Payload> ValidateAsync(string jwtToken)
        {
            var audiences = new List<string>();
            if (!string.IsNullOrEmpty(_googleClientId)) audiences.Add(_googleClientId);
            if (!string.IsNullOrEmpty(_mobileClientId)) audiences.Add(_mobileClientId);

            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = audiences
            };
            return await GoogleJsonWebSignature.ValidateAsync(jwtToken, settings);
        }
    }
}
