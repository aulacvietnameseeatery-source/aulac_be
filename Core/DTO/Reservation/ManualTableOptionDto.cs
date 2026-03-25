namespace Core.DTO.Reservation;

public class ManualTableOptionDto
{
    public string OptionId { get; set; } = null!;
    public List<long> TableIds { get; set; } = new();
    public string TableCodes { get; set; } = null!;
    public string Zone { get; set; } = null!;
    public int TotalCapacity { get; set; }
    public int ExcessCapacity { get; set; }
    public int TableCount { get; set; }
    public bool IsBestFit { get; set; }
}
