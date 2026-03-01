using System.ComponentModel.DataAnnotations;

namespace Core.DTO.LookUpValue;

/// <summary>
/// Request DTO for creating a new lookup value (zone or table type).
/// </summary>
public class CreateLookupValueRequest
{
    /// <summary>
    /// Human-readable display name (required).
    /// </summary>
    [Required(ErrorMessage = "Value name is required")]
    [MaxLength(100, ErrorMessage = "Value name cannot exceed 100 characters")]
    public string ValueName { get; set; } = null!;

    /// <summary>
    /// Optional SCREAMING_SNAKE_CASE code. Auto-generated from ValueName if not provided.
    /// </summary>
    [MaxLength(50, ErrorMessage = "Value code cannot exceed 50 characters")]
    public string? ValueCode { get; set; }

    /// <summary>
    /// Optional display sort order. Appended to end if not provided.
    /// </summary>
    public short? SortOrder { get; set; }

    /// <summary>
    /// Optional description shown as helper text in dropdowns.
    /// </summary>
    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }
}
