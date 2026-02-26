namespace Core.DTO.Order;

public class KitchenOrderDTO
{
	public long OrderId { get; set; }
	public string TableCode { get; set; } = null!;
	public string OrderStatus { get; set; } = null!;
	public DateTime? CreatedAt { get; set; }
	public List<KitchenOrderItemDTO> Items { get; set; } = new();
}

public class KitchenOrderItemDTO
{
	public long OrderItemId { get; set; }
	public string DishName { get; set; } = null!;
	public int Quantity { get; set; }
	public string ItemStatus { get; set; } = null!;
	public string? Note { get; set; }
	public string? RejectReason { get; set; }
}
