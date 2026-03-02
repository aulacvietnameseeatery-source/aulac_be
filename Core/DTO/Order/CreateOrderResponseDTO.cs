namespace Core.DTO.Order;

/// <summary>
/// Response DTO returned after successfully creating an order.
/// </summary>
public class CreateOrderResponseDTO
{
	public long OrderId { get; set; }
	public long TableId { get; set; }
	public string TableCode { get; set; } = "";
	public long CustomerId { get; set; }
	public decimal TotalAmount { get; set; }
	public string OrderStatus { get; set; } = "PENDING";
	public DateTime? CreatedAt { get; set; }
}
