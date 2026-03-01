using System.ComponentModel.DataAnnotations;

namespace Core.DTO.Table;

/// <summary>
/// Request DTO for bulk-toggling online/offline for all tables in a zone.
/// </summary>
public class BulkOnlineRequest
{
    /// <summary>
    /// The zone lookup value ID.
    /// </summary>
    [Required]
    public uint ZoneId { get; set; }

    /// <summary>
    /// Whether to set tables online (true) or offline (false).
    /// </summary>
  [Required]
    public bool IsOnline { get; set; }
}
