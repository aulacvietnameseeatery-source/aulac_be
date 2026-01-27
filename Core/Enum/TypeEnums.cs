namespace Core.Enum
{
    /// <summary>
    /// Promotion type enumeration
    /// TODO: Replace with actual business meaning
    /// </summary>
    public enum PromotionType : byte
    {

        Percentage = 1,
        FixedAmount = 2,
        BuyOneGetOne = 3,
    }

    /// <summary>
    /// Media type enumeration
    /// TODO: Replace with actual business meaning
    /// </summary>
    public enum MediaType : byte
    {

        Image = 1,
        Video = 2,
        QRCode = 3,
    }

    /// <summary>
    /// Table type enumeration
    /// TODO: Replace with actual business meaning
    /// </summary>
    public enum TableType : byte
    {

        Indoor = 1,
        Outdoor = 2,
        Private = 3,
        Bar = 4,
    }

    /// <summary>
    /// Reservation source enumeration
    /// TODO: Replace with actual business meaning
    /// </summary>
    public enum ReservationSource : byte
    {

        Phone = 1,
        Website = 2,
        WalkIn = 3,
        Mobile = 4,
        ThirdParty = 5,
    }

    /// <summary>
    /// Order source enumeration
    /// TODO: Replace with actual business meaning
    /// </summary>
    public enum OrderSource : byte
    {

        DineIn = 1,
        Takeaway = 2,
        Delivery = 3,
        Online = 4,
    }

    /// <summary>
    /// Payment method enumeration
    /// TODO: Replace with actual business meaning
    /// </summary>
    public enum PaymentMethod : byte
    {

        Cash = 1,
        Card = 2,
        BankTransfer = 3,
        EWallet = 4,
    }

    /// <summary>
    /// Severity level enumeration
    /// TODO: Replace with actual business meaning
    /// </summary>
    public enum SeverityLevel : byte
    {

        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4,
    }

    /// <summary>
    /// Inventory transaction type enumeration
    /// Based on schema: 1=IN, 2=OUT, 3=ADJUST
    /// </summary>
    public enum InventoryTransactionType : byte
    {

        In = 1,
        Out = 2,
        Adjust = 3,
    }
}
