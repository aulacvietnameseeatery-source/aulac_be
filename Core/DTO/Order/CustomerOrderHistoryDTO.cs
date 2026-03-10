namespace Core.DTO.Order;

/// <summary>
/// Full order history response returned to the customer for a given table.
/// </summary>
public class CustomerOrderHistoryDTO
{
	public string TableCode { get; set; } = "";

	/// <summary>Total number of individual item quantities.</summary>
	public int TotalItems { get; set; }

	/// <summary>Sum of price × quantity for all active items (excludes rejected and cancelled).</summary>
	public decimal EstimatedTotal { get; set; }

	/// <summary>All order items from all orders, ordered from most-recent first.</summary>
	public List<OrderItemDTO> Items { get; set; } = new();
}
