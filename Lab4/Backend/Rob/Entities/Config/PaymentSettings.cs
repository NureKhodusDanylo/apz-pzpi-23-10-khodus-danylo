namespace Entities.Config
{
    public class PaymentSettings
    {
        public PayPalSettings PayPal { get; set; } = new();
        public StripeSettings Stripe { get; set; } = new();
        public GooglePaySettings GooglePay { get; set; } = new();
    }

    public class PayPalSettings
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string Mode { get; set; } = "sandbox"; // sandbox or live
    }

    public class StripeSettings
    {
        public string SecretKey { get; set; } = string.Empty;
        public string PublishableKey { get; set; } = string.Empty;
        public string WebhookSecret { get; set; } = string.Empty;
    }

    public class GooglePaySettings
    {
        public string MerchantId { get; set; } = string.Empty;
        public string MerchantName { get; set; } = string.Empty;
        public string Gateway { get; set; } = string.Empty; // stripe, adyen, etc.
        public string GatewayMerchantId { get; set; } = string.Empty;
    }
}
