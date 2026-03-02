using System.ComponentModel.DataAnnotations;

namespace Core.DTO.LookUpValue;

/// <summary>
/// Request DTO for updating an existing lookup value (zone, table type, etc.).
/// All fields are optional — only provided fields are updated.
/// </summary>
public class UpdateLookupValueRequest
{
    /// <summary>
    /// Updated human-readable display name.
    /// </summary>
    [MaxLength(100, ErrorMessage = "Value name cannot exceed 100 characters")]
    public string? ValueName { get; set; }

    /// <summary>
    /// Updated display sort order.
    /// </summary>
    public short? SortOrder { get; set; }

    /// <summary>
    /// Per-locale display names. Replaces the entire i18n map if provided.
    /// </summary>
    public LookupI18nMap? I18n { get; set; }

    /// <summary>
    /// Per-locale descriptions. Replaces the entire descriptionI18n map if provided.
    /// </summary>
    public LookupI18nMap? DescriptionI18n { get; set; }
}
