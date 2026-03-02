using System.ComponentModel.DataAnnotations;

namespace Core.DTO.Table;

public class UpdateTableRequest
{
    [MaxLength(20)]
    public string? TableCode { get; set; }

    [Range(1, 50)]
  public int? Capacity { get; set; }

    public bool? IsOnline { get; set; }
    public uint? StatusLvId { get; set; }
    public uint? TypeLvId { get; set; }
    public uint? ZoneLvId { get; set; }
}
