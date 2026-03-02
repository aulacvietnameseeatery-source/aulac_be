using System.ComponentModel.DataAnnotations;

namespace Core.DTO.LookUpValue;

/// <summary>
/// Request DTO for creating a new lookup value (zone, table type, etc.).
/// </summary>
public class CreateLookupValueRequest
{
    /// <summary>
    /// Human-readable display name (required). Used as primary fallback and for ValueCode generation.
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
    /// Per-locale display names. If provided, these override the flat ValueName for each locale.
    /// </summary>
    public LookupI18nMap? I18n { get; set; }

    /// <summary>
    /// Per-locale descriptions shown as helper text in dropdowns.
    /// </summary>
    public LookupI18nMap? DescriptionI18n { get; set; }
}

/// <summary>
/// Translatable text map with per-locale values.
/// </summary>
public class LookupI18nMap
{
    public string? Vi { get; set; }
    public string? En { get; set; }
    public string? Fr { get; set; }
}
