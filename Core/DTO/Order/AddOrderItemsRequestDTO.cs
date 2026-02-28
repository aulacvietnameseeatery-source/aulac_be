namespace Core.DTO.Order;

/// <summary>
/// Request DTO for adding more items to an existing order (same table / same customer session).
/// </summary>
public class AddOrderItemsRequestDTO
{
	/// <summary>Items to append to the existing order.</summary>
	public List<CreateOrderItemDTO> Items { get; set; } = new();
}
