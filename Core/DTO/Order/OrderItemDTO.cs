namespace Core.DTO.Order;

public class OrderItemDTO
{
	public long OrderItemId { get; set; }
	public long DishId { get; set; }
	public string DishName { get; set; } = "";
	public int Quantity { get; set; }
	public decimal Price { get; set; }
	public string ItemStatus { get; set; } = "";
	public string? RejectReason { get; set; }
}
