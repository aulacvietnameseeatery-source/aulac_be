using System.ComponentModel.DataAnnotations;

namespace Core.DTO.Order;

public class UpdateOrderItemStatusDTO
{
	/// <summary>
	/// Target status code: IN_PROGRESS, READY, SERVED, or REJECTED
	/// </summary>
	[Required]
	public string Status { get; set; } = null!;

	/// <summary>
	/// Required when Status is REJECTED
	/// </summary>
	public string? RejectReason { get; set; }
}
