namespace Core.DTO.Table;

/// <summary>
/// Response DTO for QR code regeneration.
/// </summary>
public class QrCodeDto
{
    public string? QrCodeUrl { get; set; }
    public string? QrCodeImageUrl { get; set; }
}
