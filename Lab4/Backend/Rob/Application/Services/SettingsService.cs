using Application.Abstractions.Interfaces;
using Application.DTOs.AdminDTOs;
using Entities.Models;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Application.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly MyDbContext _context;

        public SettingsService(MyDbContext context)
        {
            _context = context;
        }

        public async Task<PricingSettingsDTO> GetPricingSettingsAsync()
        {
            return new PricingSettingsDTO
            {
                BasePrice = await GetSettingAsync("Pricing:BasePrice", 50m),
                PricePerKg = await GetSettingAsync("Pricing:PricePerKg", 10m),
                DistanceMultiplier = await GetSettingAsync("Pricing:DistanceMultiplier", 1.5m),
                RushDeliveryMarkup = await GetSettingAsync("Pricing:RushDeliveryMarkup", 1.25m)
            };
        }

        public async Task<bool> UpdatePricingSettingsAsync(UpdatePricingDTO updateDto)
        {
            if (updateDto.BasePrice.HasValue)
                await SaveSettingAsync("Pricing:BasePrice", updateDto.BasePrice.Value.ToString(CultureInfo.InvariantCulture));
            
            if (updateDto.PricePerKg.HasValue)
                await SaveSettingAsync("Pricing:PricePerKg", updateDto.PricePerKg.Value.ToString(CultureInfo.InvariantCulture));

            if (updateDto.DistanceMultiplier.HasValue)
                await SaveSettingAsync("Pricing:DistanceMultiplier", updateDto.DistanceMultiplier.Value.ToString(CultureInfo.InvariantCulture));

            if (updateDto.RushDeliveryMarkup.HasValue)
                await SaveSettingAsync("Pricing:RushDeliveryMarkup", updateDto.RushDeliveryMarkup.Value.ToString(CultureInfo.InvariantCulture));

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<string> GetSettingAsync(string key, string defaultValue)
        {
            var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == key);
            return setting?.Value ?? defaultValue;
        }

        public async Task<T> GetSettingAsync<T>(string key, T defaultValue)
        {
            var valueStr = await GetSettingAsync(key, string.Empty);
            if (string.IsNullOrEmpty(valueStr))
                return defaultValue;

            try
            {
                return (T)Convert.ChangeType(valueStr, typeof(T), CultureInfo.InvariantCulture);
            }
            catch
            {
                return defaultValue;
            }
        }

        private async Task SaveSettingAsync(string key, string value)
        {
            var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == key);
            if (setting == null)
            {
                setting = new SystemSetting { Key = key, Value = value };
                await _context.SystemSettings.AddAsync(setting);
            }
            else
            {
                setting.Value = value;
                setting.UpdatedAt = DateTime.UtcNow;
                _context.SystemSettings.Update(setting);
            }
        }
    }
}
