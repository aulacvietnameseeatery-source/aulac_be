namespace Core.DTO.Dish;

/// <summary>
/// Dish detail response DTO
/// </summary>
public class DishDetailDto
{
    public long DishId { get; set; }
    public string DishName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    public string? Slogan { get; set; }
    public int? Calories { get; set; }
    public int? PrepTimeMinutes { get; set; }
    public int? CookTimeMinutes { get; set; }
    public List<string> ImageUrls { get; set; } = new();
}
