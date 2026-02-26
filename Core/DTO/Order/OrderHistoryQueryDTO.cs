namespace Core.DTO.Order;

public class OrderHistoryQueryDTO
{
	public string? Search { get; set; }        // search by OrderId, CustomerName, StaffName, TableCode
	public int PageIndex { get; set; } = 1;
	public int PageSize { get; set; } = 10;
	public uint? OrderStatusLvId { get; set; }
	public DateTime? FromDate { get; set; }
	public DateTime? ToDate { get; set; }
}
