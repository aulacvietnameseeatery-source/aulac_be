namespace Core.DTO.Order;

public class OrderHistoryDTO
{
	public long OrderId { get; set; }
	public long? TableId { get; set; }
	public string TableCode { get; set; } = "";
	public long? StaffId { get; set; }
	public string StaffName { get; set; } = "";
	public long CustomerId { get; set; }
	public string? CustomerName { get; set; }
	public decimal TotalAmount { get; set; }
	public decimal? TipAmount { get; set; }
	public string OrderStatus { get; set; } = "";
	public string Source { get; set; } = "";
	public DateTime? CreatedAt { get; set; }
	public DateTime? UpdatedAt { get; set; }
	public bool IsPaid { get; set; }
	public decimal TaxAmount { get; set; }
	public long? TaxId { get; set; }
	public List<OrderItemDTO> OrderItems { get; set; } = new();
	public int ItemCount => OrderItems.Count;
}


