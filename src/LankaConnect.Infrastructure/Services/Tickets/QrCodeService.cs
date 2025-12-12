using LankaConnect.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using QRCoder;

namespace LankaConnect.Infrastructure.Services.Tickets;

/// <summary>
/// Phase 6A.24: QR code generation service using QRCoder library
/// </summary>
public class QrCodeService : IQrCodeService
{
    private readonly ILogger<QrCodeService> _logger;

    public QrCodeService(ILogger<QrCodeService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public byte[] GenerateQrCode(string data, int pixelsPerModule = 10)
    {
        try
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);

            var qrCodeImage = qrCode.GetGraphic(pixelsPerModule);

            _logger.LogDebug("Generated QR code with {Bytes} bytes for data length {DataLength}",
                qrCodeImage.Length, data.Length);

            return qrCodeImage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate QR code for data length {DataLength}", data.Length);
            throw;
        }
    }

    /// <inheritdoc />
    public string GenerateQrCodeBase64(string data, int pixelsPerModule = 10)
    {
        var qrCodeBytes = GenerateQrCode(data, pixelsPerModule);
        return Convert.ToBase64String(qrCodeBytes);
    }
}
