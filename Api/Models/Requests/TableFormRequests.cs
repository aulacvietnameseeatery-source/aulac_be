using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace API.Models.Requests;

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

    public List<IFormFile> Images { get; set; } = [];
}

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
    public List<IFormFile> Images { get; set; } = [];
    public string? RemovedImageIds { get; set; }

    public List<long> ParseRemovedImageIds()
    {
        return string.IsNullOrWhiteSpace(RemovedImageIds)
            ? []
            : RemovedImageIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(value => long.TryParse(value, out var id) ? id : -1L)
                .Where(id => id > 0)
                .ToList();
    }
}
