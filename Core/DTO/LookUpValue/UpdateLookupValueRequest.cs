using System.ComponentModel.DataAnnotations;

namespace Core.DTO.LookUpValue;

/// <summary>
/// Request DTO for updating an existing lookup value (zone or table type).
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
    /// Updated description.
    /// </summary>
    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }
}
