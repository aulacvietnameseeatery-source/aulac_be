namespace Core.DTO.Report
{
    public class RecentOrderDto
{
    public string Id { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty; 
    public string Customer { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
}
}