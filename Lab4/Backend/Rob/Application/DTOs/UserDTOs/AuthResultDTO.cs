namespace Application.DTOs.UserDTOs
{
    public class AuthResultDTO
    {
        /// <summary>
        /// "Success", "NeedsAdditionalInfo", "Error"
        /// </summary>
        public string Status { get; set; }
        public string? Token { get; set; }
        public string? Message { get; set; }

        // Fields populated when Status == "NeedsAdditionalInfo" (Google flow)
        public string? GoogleId { get; set; }
        public string? Email { get; set; }
        public string? UserName { get; set; }
    }
}
