namespace Core.DTO.Dish;

/// <summary>
/// Response DTO containing dish status information.
/// </summary>
public record DishStatusDto
{
    /// <summary>
    /// The dish ID.
    /// </summary>
    public long DishId { get; init; }

    /// <summary>
    /// The dish name.
    /// </summary>
    public string DishName { get; init; } = string.Empty;

    /// <summary>
    /// The status code enum value.
    /// </summary>
    public DishStatusCode StatusCode { get; init; }

    /// <summary>
    /// The lookup value ID for the status.
    /// </summary>
    public uint StatusId { get; init; }

    /// <summary>
    /// When the status was updated.
    /// </summary>
    public DateTime UpdatedAt { get; init; }
}
