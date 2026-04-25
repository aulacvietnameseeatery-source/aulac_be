using Core.DTO.Customer;

namespace Core.DTO.Order;

/// <summary>
/// Single-call staff endpoint: adjust existing items, add new items, optionally update customer.
/// PUT /api/orders/staff/{id}/items
/// </summary>
public class UpdateOrderItemsRequest
{
    /// <summary>Adjustments to existing order items (quantity changes / removals).</summary>
    public List<OrderItemAdjustmentDto> Adjustments { get; set; } = new();

    /// <summary>Brand-new dishes to append to the order.</summary>
    public List<CreateOrderItemDto> NewItems { get; set; } = new();

    /// <summary>Optional — change the customer linked to this order.</summary>
    public OrderCustomerDto? Customer { get; set; }
}

public class OrderItemAdjustmentDto
{
    /// <summary>ID of the existing order item to adjust.</summary>
    public long OrderItemId { get; set; }

    /// <summary>
    /// Target quantity. Set to 0 to remove (marks item REJECTED).
    /// Must be &lt;= current quantity when item is SERVED.
    /// </summary>
    public int NewQuantity { get; set; }

    /// <summary>Required when item is SERVED or when removing an item (NewQuantity == 0).</summary>
    public string? Reason { get; set; }

    /// <summary>
    /// For CREATED items only — replaces the item's note.
    /// Ignored for SERVED items (their note is managed via audit trail).
    /// </summary>
    public string? Note { get; set; }
}
