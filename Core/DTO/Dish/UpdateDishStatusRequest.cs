using System.ComponentModel.DataAnnotations;

namespace Core.DTO.Dish;

/// <summary>
/// Request DTO for updating dish status.
/// </summary>
public class UpdateDishStatusRequest
{
    /// <summary>
    /// The new status code for the dish.
    /// Valid values: AVAILABLE, OUT_OF_STOCK, HIDDEN
    /// </summary>
    [Required(ErrorMessage = "Status is required")]
    public DishStatusCode Status { get; set; }
}
