namespace Application.DTOs.PartnerDTOs
{
    public class PartnerResponseDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ContactPerson { get; set; }
        public string PhoneNumber { get; set; }
        public int NodeId { get; set; }
        public string? LocationName { get; set; }
    }
}
