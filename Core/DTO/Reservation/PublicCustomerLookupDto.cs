namespace Core.DTO.Reservation;

public class PublicCustomerLookupDto
{
    public long CustomerId { get; set; }
    public string Phone { get; set; } = null!;
    public string? MaskedName { get; set; }
    public string? MaskedEmail { get; set; }
}