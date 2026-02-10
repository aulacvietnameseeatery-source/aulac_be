public enum AccountStatusCode
{
    ACTIVE,
    INACTIVE,
    LOCKED
}

public enum InventoryTxTypeCode
{
    IN,
    OUT,
    ADJUST
}

public enum InventoryTxStatusCode
{
    DRAFT,
    PENDING_APPROVAL,
    COMPLETED,
    CANCELLED
}

public enum TableStatusCode
{
    AVAILABLE,
    OCCUPIED,
    RESERVED,
    LOCKED
}

public enum ReservationStatusCode
{
    PENDING,
    CONFIRMED,
    CHECKED_IN,
    CANCELLED,
    NO_SHOW
}

public enum OrderStatusCode
{
    PENDING,
    IN_PROGRESS,
    COMPLETED,
    CANCELLED
}

public enum OrderItemStatusCode
{
    CREATED,
    IN_PROGRESS,
    READY,
    SERVED,
    REJECTED
}

public enum DishStatusCode
{
    AVAILABLE,
    OUT_OF_STOCK,
    HIDDEN
}

public enum PromotionStatusCode
{
    SCHEDULED,
    ACTIVE,
    EXPIRED,
    DISABLED
}

public enum TableZoneCode
{
    INDOOR,
    OUTDOOR,
    ROOFTOP
}

public enum RoleStatusCode
{
    ACTIVE,
    INACTIVE
}
