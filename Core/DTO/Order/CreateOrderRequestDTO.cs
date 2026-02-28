namespace Core.DTO.Order;

/// <summary>
/// Request DTO for creating an order from the customer-facing menu.
/// </summary>
public class CreateOrderRequestDTO
{
	/// <summary>Table code (e.g. "TB-002") – resolved to table_id on the server.</summary>
	public string TableCode { get; set; } = null!;

	/// <summary>
	/// When true, the customer skipped the info form.
	/// The server will use the shared guest account (customer_id = 68).
	/// </summary>
	public bool IsGuest { get; set; }

	/// <summary>Required when IsGuest = false. Phone is mandatory.</summary>
	public string? CustomerPhone { get; set; }

	/// <summary>Optional – used for find-or-create when filling in customer info.</summary>
	public string? CustomerFullName { get; set; }

	/// <summary>Optional – used for find-or-create when filling in customer info.</summary>
	public string? CustomerEmail { get; set; }

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
