using Core.Interface.Service.FileStorage;
using QRCoder;

namespace Infa.Service;

/// <summary>
/// QRCoder-backed implementation of <see cref="IQrCodeGenerator"/>.
/// Produces a PNG byte array at a pixel-per-module size of 10 (≈ 250 px for a standard QR).
/// </summary>
public sealed class QrCodeGeneratorService : IQrCodeGenerator
{
    private const int PixelsPerModule = 10;

    /// <inheritdoc />
    public byte[] GeneratePng(string content)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
        using var code = new PngByteQRCode(data);
        return code.GetGraphic(PixelsPerModule);
    }
}
