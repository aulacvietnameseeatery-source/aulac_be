using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Core.DTO.Table;

/// <summary>
/// Multipart/form-data request for creating a new table with optional images.
/// Send table fields as individual form fields alongside the image files.
/// </summary>
public class CreateTableFormRequest
{
    [Required, MaxLength(20)]
    public string TableCode { get; set; } = null!;

    [Range(1, 50)]
    public int Capacity { get; set; }

    public bool IsOnline { get; set; } = false;

    [Required]
    public uint StatusLvId { get; set; }

    [Required]
    public uint TypeLvId { get; set; }

    [Required]
    public uint ZoneLvId { get; set; }

    /// <summary>
    /// Optional image files to attach during creation. Max 5 files, 5 MB each, image types only.
    /// </summary>
    public List<IFormFile> Images { get; set; } = [];
}

/// <summary>
/// Multipart/form-data request for updating a table with optional image changes.
/// All data fields are optional — only provided fields are updated.
/// </summary>
public class UpdateTableFormRequest
{
    [MaxLength(20)]
    public string? TableCode { get; set; }

    [Range(1, 50)]
    public int? Capacity { get; set; }

    public bool? IsOnline { get; set; }

    public uint? StatusLvId { get; set; }

    public uint? TypeLvId { get; set; }

    public uint? ZoneLvId { get; set; }

    /// <summary>
    /// New image files to add. Max 5 files total across add+existing, 5 MB each, image types only.
    /// </summary>
    public List<IFormFile> Images { get; set; } = [];

    /// <summary>
    /// Comma-separated list of MediaAsset IDs to remove.
    /// Example: "10,11,14"
    /// </summary>
    public string? RemovedImageIds { get; set; }

    /// <summary>
    /// Parses RemovedImageIds into a list of longs. Returns empty list when null or blank.
    /// </summary>
    [JsonIgnore]
    public List<long> ParsedRemovedImageIds =>
  string.IsNullOrWhiteSpace(RemovedImageIds)
            ? []
         : RemovedImageIds
             .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
              .Select(s => long.TryParse(s, out var id) ? id : -1L)
       .Where(id => id > 0)
    .ToList();
}
