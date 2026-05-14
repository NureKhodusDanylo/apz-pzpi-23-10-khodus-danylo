namespace Application.DTOs.AdminDTOs
{
    public class PricingSettingsDTO
    {
        public decimal BasePrice { get; set; }
        public decimal PricePerKg { get; set; }
        public decimal DistanceMultiplier { get; set; }
        public decimal RushDeliveryMarkup { get; set; }
    }

    public class UpdatePricingDTO
    {
        public decimal? BasePrice { get; set; }
        public decimal? PricePerKg { get; set; }
        public decimal? DistanceMultiplier { get; set; }
        public decimal? RushDeliveryMarkup { get; set; }
    }
}
