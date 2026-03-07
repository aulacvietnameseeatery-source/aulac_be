namespace Core.Interface.Service.FileStorage;

/// <summary>
/// Generates a QR code PNG image from a content string.
/// </summary>
public interface IQrCodeGenerator
{
    /// <summary>
    /// Renders <paramref name="content"/> as a PNG QR code and returns the raw bytes.
    /// </summary>
    byte[] GeneratePng(string content);
}
