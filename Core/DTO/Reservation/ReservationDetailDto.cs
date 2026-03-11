namespace Core.DTO.Reservation;

/// <summary>
/// DTO chi tiết đặt bàn cho quản lý (Admin/Staff).
/// Hiển thị đầy đủ thông tin reservation bao gồm bàn, nguồn, trạng thái.
/// </summary>
public class ReservationDetailDto
{
    public long ReservationId { get; set; }

    // Thông tin khách hàng
    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int PartySize { get; set; }

    // Thời gian
    public DateTime ReservedTime { get; set; }
    public DateTime? CreatedAt { get; set; }

    // Trạng thái
    public long StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string StatusCode { get; set; } = string.Empty;

    // Nguồn đặt bàn
    public long SourceId { get; set; }
    public string SourceName { get; set; } = string.Empty;
    public string SourceCode { get; set; } = string.Empty;

    // Danh sách bàn đã đặt
    public List<ReservationTableDto> Tables { get; set; } = new();
}

/// <summary>
/// Thông tin bàn trong reservation detail.
/// </summary>
public class ReservationTableDto
{
    public long TableId { get; set; }
    public string TableCode { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string TableType { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;
}
