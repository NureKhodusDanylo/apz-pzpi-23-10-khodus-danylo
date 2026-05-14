namespace Application.DTOs.AdminDTOs;

/// <summary>
/// DTO for creating a new admin registration key
/// </summary>
public class CreateAdminKeyDTO
{
    /// <summary>
    /// Optional expiration date for the key (null = no expiration)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Optional description or note about this key
    /// </summary>
    public string? Description { get; set; }
}
