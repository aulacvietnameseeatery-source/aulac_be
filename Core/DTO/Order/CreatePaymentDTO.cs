namespace Core.DTO.Order;

public class CreatePaymentDTO
{
    public long OrderId { get; set; }
    public decimal ReceivedAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty; // CASH, CARD, QR
    public string? Note { get; set; }
    public decimal? TipAmount { get; set; }
}
