namespace Core.DTO.Table;

public class TableDetailDto : TableManagementDto
{
    public string? QrCodeUrl { get; set; }
    public string? QrCodeImageUrl { get; set; }
    public List<TableMediaDto> Images { get; set; } = new();
    public int ActiveOrdersCount { get; set; }
    public bool HasErrors { get; set; }
    public List<UpcomingReservationDto> UpcomingReservations { get; set; } = new();
}

public class TableMediaDto
{
    public long MediaId { get; set; }
  public string Url { get; set; } = null!;
 public bool IsPrimary { get; set; }
}

public class UpcomingReservationDto
{
    public long ReservationId { get; set; }
    public string GuestName { get; set; } = null!;
    public int Pax { get; set; }
    public DateTime ReservedTime { get; set; }
    public string StatusCode { get; set; } = null!;
}
