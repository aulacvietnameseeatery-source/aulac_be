namespace Core.DTO.Order;

/// <summary>
/// Request DTO for creating an order from the customer-facing menu.
/// Customer is always treated as guest (customer_id = 68).
/// </summary>
public class CreateOrderRequestDTO
{
	/// <summary>Table code (e.g. "TB-002") – resolved to table_id on the server.</summary>
	public string TableCode { get; set; } = null!;

	/// <summary>Cart items to insert as order_item rows.</summary>
	public List<CreateOrderItemDTO> Items { get; set; } = new();
}

public class CreateOrderItemDTO
{
	public long DishId { get; set; }
	public int Quantity { get; set; }
	public decimal Price { get; set; }
	public string? Note { get; set; }
}
