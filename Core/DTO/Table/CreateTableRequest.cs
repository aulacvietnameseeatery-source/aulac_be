using System.ComponentModel.DataAnnotations;

namespace Core.DTO.Table;

public class CreateTableRequest
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
}
