using System.ComponentModel.DataAnnotations;

namespace Core.DTO.Order;

public class UpdateOrderStatusDTO
{
    [Required]
    public string Status { get; set; } = string.Empty;
}
