using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Phase 6A.35/6A.37: Service for providing email branding assets (logo, banners) as embedded resources.
/// These assets are embedded inline in emails using CID (Content-ID) to ensure they display
/// immediately without requiring users to click "display images" in email clients.
/// </summary>
public interface IEmailBrandingService
{
    /// <summary>
    /// Gets the LankaConnect logo as bytes for CID embedding in emails.
    /// </summary>
    /// <returns>Logo image bytes</returns>
    Task<Result<EmailBrandingAsset>> GetLogoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the email banner image as bytes for CID embedding.
    /// The banner matches the LankaConnect landing page style with gradient and cross pattern.
    /// </summary>
    /// <returns>Banner image bytes</returns>
    Task<Result<EmailBrandingAsset>> GetBannerAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 6A.37: Gets the email header banner (650x120px) with gradient and "Registration Confirmed!" text.
    /// </summary>
    Task<Result<EmailBrandingAsset>> GetHeaderBannerAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 6A.37: Gets the email footer banner (650x180px) with logo and branding.
    /// </summary>
    Task<Result<EmailBrandingAsset>> GetFooterBannerAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 6A.37: Downloads an image from a URL and returns it as a branding asset for CID embedding.
    /// Used for event images that need to be embedded inline.
    /// </summary>
    Task<Result<EmailBrandingAsset>> DownloadImageAsync(string imageUrl, string contentId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a branding asset (image) for email embedding
/// </summary>
public class EmailBrandingAsset
{
    /// <summary>
    /// The image content as bytes
    /// </summary>
    public byte[] Content { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// MIME type of the image (e.g., "image/png")
    /// </summary>
    public string ContentType { get; set; } = "image/png";

    /// <summary>
    /// Suggested filename for the attachment
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Content-ID for CID reference in HTML (e.g., "lankaconnect-logo")
    /// </summary>
    public string ContentId { get; set; } = string.Empty;
}
