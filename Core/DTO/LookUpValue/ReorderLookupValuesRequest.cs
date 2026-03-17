using System.ComponentModel.DataAnnotations;

namespace Core.DTO.LookUpValue;

/// <summary>
/// Request DTO for reordering a batch of lookup values.
/// </summary>
public class ReorderLookupValuesRequest
{
    /// <summary>
    /// A list of items to reorder, containing the value ID and the desired sort order.
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "At least one item must be provided")]
    public List<ReorderLookupItem> Items { get; set; } = new();
}

/// <summary>
/// Represents a single lookup value and its new sort order.
/// </summary>
public class ReorderLookupItem
{
    /// <summary>
    /// The ID of the lookup value.
    /// </summary>
    [Required]
    public uint ValueId { get; set; }

    /// <summary>
    /// The new sort order for the lookup value.
    /// </summary>
    [Required]
    public short SortOrder { get; set; }
}
