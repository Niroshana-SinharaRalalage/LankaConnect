namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Phase 6A.24: Service interface for generating QR codes
/// </summary>
public interface IQrCodeService
{
    /// <summary>
    /// Generates a QR code image from the provided data
    /// </summary>
    /// <param name="data">The data to encode in the QR code</param>
    /// <param name="pixelsPerModule">Size of each QR module in pixels (default: 10)</param>
    /// <returns>PNG image bytes of the QR code</returns>
    byte[] GenerateQrCode(string data, int pixelsPerModule = 10);

    /// <summary>
    /// Generates a QR code image as a base64 string
    /// </summary>
    /// <param name="data">The data to encode in the QR code</param>
    /// <param name="pixelsPerModule">Size of each QR module in pixels (default: 10)</param>
    /// <returns>Base64 encoded PNG image</returns>
    string GenerateQrCodeBase64(string data, int pixelsPerModule = 10);
}
