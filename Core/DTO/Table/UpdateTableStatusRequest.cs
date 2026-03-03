using System.ComponentModel.DataAnnotations;

namespace Core.DTO.Table;

public class UpdateTableStatusRequest
{
    [Required]
    public uint StatusLvId { get; set; }
}
