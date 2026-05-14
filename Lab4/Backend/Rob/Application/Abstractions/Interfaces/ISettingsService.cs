using Application.DTOs.AdminDTOs;

namespace Application.Abstractions.Interfaces
{
    public interface ISettingsService
    {
        Task<PricingSettingsDTO> GetPricingSettingsAsync();
        Task<bool> UpdatePricingSettingsAsync(UpdatePricingDTO updateDto);
        Task<string> GetSettingAsync(string key, string defaultValue);
        Task<T> GetSettingAsync<T>(string key, T defaultValue);
    }
}
