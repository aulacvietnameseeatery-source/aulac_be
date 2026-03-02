namespace Core.DTO.Order;

/// <summary>
/// A single "round" of ordering (one Order entity) in the customer history view.
/// </summary>
public class CustomerOrderRoundDTO
{
	public long OrderId { get; set; }

	/// <summary>1-based round number ordered by creation time.</summary>
	public int RoundNumber { get; set; }

	public DateTime? CreatedAt { get; set; }

	/// <summary>Order status string, e.g. PENDING / IN_PROGRESS / COMPLETED / CANCELLED.</summary>
	public string OrderStatus { get; set; } = "";

	public decimal TotalAmount { get; set; }

	public List<OrderItemDTO> Items { get; set; } = new();
}

/// <summary>
/// Full order history response returned to the customer for a given table.
/// </summary>
public class CustomerOrderHistoryDTO
{
	public string TableCode { get; set; } = "";

	/// <summary>Total number of individual item quantities across all rounds.</summary>
	public int TotalItems { get; set; }

	/// <summary>Sum of price × quantity for all items across all rounds.</summary>
	public decimal EstimatedTotal { get; set; }

	/// <summary>Rounds ordered from most-recent first.</summary>
	public List<CustomerOrderRoundDTO> Rounds { get; set; } = new();
}
