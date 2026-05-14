using Entities.Interfaces;

namespace Entities.Models;

/// <summary>
/// Represents a one-time admin registration key
/// </summary>
public class AdminKey : IDbEntity
{
    /// <summary>
    /// Unique identifier for the admin key
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The actual key code that user must provide during registration
    /// </summary>
    public string KeyCode { get; set; } = string.Empty;

    /// <summary>
    /// When the key was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the key expires (optional, can be null for no expiration)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Whether the key has been used
    /// </summary>
    public bool IsUsed { get; set; }

    /// <summary>
    /// When the key was used
    /// </summary>
    public DateTime? UsedAt { get; set; }

    /// <summary>
    /// The user who used this key (null if not yet used)
    /// </summary>
    public int? UsedByUserId { get; set; }

    /// <summary>
    /// Navigation property to the user who used this key
    /// </summary>
    public User? UsedByUser { get; set; }

    /// <summary>
    /// The admin who created this key
    /// </summary>
    public int CreatedByAdminId { get; set; }

    /// <summary>
    /// Navigation property to the admin who created this key
    /// </summary>
    public User CreatedByAdmin { get; set; } = null!;

    /// <summary>
    /// Optional description or note about this key
    /// </summary>
    public string? Description { get; set; }
}
