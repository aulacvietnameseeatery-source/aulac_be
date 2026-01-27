namespace Core.Enum
{
    /// <summary>
    /// Account status enumeration
    /// </summary>
    public enum AccountStatus : byte
    {
        Active = 1,
        Inactive = 2,
        Locked = 3
    }

    /// <summary>
    /// Table status enumeration
    /// </summary>
    public enum TableStatus : byte
    {
        Available = 1,
        Occupied = 2,
        Reserved = 3,
        Locked = 4
    }

    /// <summary>
    /// Reservation status enumeration
    /// </summary>
    public enum ReservationStatus : byte
    {
        Pending = 1,
        Confirmed = 2,
        CheckedIn = 3,
        Cancelled = 4,
        NoShow = 5
    }

    /// <summary>
    /// Order status enumeration
    /// </summary>
    public enum OrderStatus : byte
    {
        Pending = 1,
        InProgress = 2,
        Completed = 3,
        Cancelled = 4
    }

    /// <summary>
    /// Order item status enumeration
    /// </summary>
    public enum OrderItemStatus : byte
    {
        Created = 1,
        InProgress = 2,
        Ready = 3,
        Served = 4,
        Rejected = 5
    }

    /// <summary>
    /// Invoice status enumeration (for future use)
    /// </summary>
    public enum InvoiceStatus : byte
    {
        Pending = 1,
        Paid = 2,
        Cancelled = 3,
        Refunded = 4
    }

    /// <summary>
    /// Dish status enumeration
    /// </summary>
    public enum DishStatus : byte
    {
        Available = 1,
        OutOfStock = 2,
        Hidden = 3
    }

    /// <summary>
    /// Purchase/Inventory transaction status enumeration
    /// </summary>
    public enum PurchaseStatus : byte
    {
        Draft = 1,
        PendingApproval = 2,
        Completed = 3,
        Cancelled = 4
    }

    /// <summary>
    /// Promotion status enumeration
    /// </summary>
    public enum PromotionStatus : byte
    {
        Scheduled = 1,
        Active = 2,
        Expired = 3,
        Disabled = 4
    }
}
