namespace Application.DTOs.AdminDTOs;

/// <summary>
/// DTO for admin registration key
/// </summary>
public class AdminKeyDTO
{
    public int Id { get; set; }
    public string KeyCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }
    public int? UsedByUserId { get; set; }
    public string? UsedByUserName { get; set; }
    public int CreatedByAdminId { get; set; }
    public string CreatedByAdminName { get; set; } = string.Empty;
    public string? Description { get; set; }
}
