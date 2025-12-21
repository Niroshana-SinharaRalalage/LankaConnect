using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Infrastructure.Email.Services;

/// <summary>
/// Phase 6A.35: Implementation of email branding service for providing logo and banner images.
/// Images are embedded inline in emails using CID (Content-ID) to ensure they display
/// immediately without requiring users to click "display images" in email clients.
///
/// Note: For production, consider hosting optimized images on Azure blob storage
/// and caching them in memory to reduce email size and improve performance.
/// </summary>
public class EmailBrandingService : IEmailBrandingService
{
    private readonly ILogger<EmailBrandingService> _logger;

    // Content-IDs for CID embedding - these are referenced in email templates using src="cid:{ContentId}"
    private const string LogoContentId = "lankaconnect-logo";
    private const string BannerContentId = "lankaconnect-banner";

    // LankaConnect logo as base64 (70x70 optimized version for email)
    // This is a small placeholder - in production, use a properly optimized logo
    // The actual logo from web/public/images/lankaconnect-logo.png is ~1.5MB which is too large
    // For now, we'll generate a simple SVG-based logo that matches the brand colors
    private static readonly string LogoBase64 = GenerateLogoBase64();

    public EmailBrandingService(ILogger<EmailBrandingService> logger)
    {
        _logger = logger;
    }

    public Task<Result<EmailBrandingAsset>> GetLogoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var logoBytes = Convert.FromBase64String(LogoBase64);

            var asset = new EmailBrandingAsset
            {
                Content = logoBytes,
                ContentType = "image/png",
                FileName = "lankaconnect-logo.png",
                ContentId = LogoContentId
            };

            _logger.LogDebug("Retrieved logo branding asset, size: {Size} bytes", logoBytes.Length);
            return Task.FromResult(Result<EmailBrandingAsset>.Success(asset));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get logo branding asset");
            return Task.FromResult(Result<EmailBrandingAsset>.Failure($"Failed to get logo: {ex.Message}"));
        }
    }

    public Task<Result<EmailBrandingAsset>> GetBannerAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Generate a simple banner image that matches the LankaConnect gradient
            // In production, this should be a pre-rendered image from Azure blob storage
            var bannerBytes = GenerateBannerImage();

            var asset = new EmailBrandingAsset
            {
                Content = bannerBytes,
                ContentType = "image/png",
                FileName = "lankaconnect-banner.png",
                ContentId = BannerContentId
            };

            _logger.LogDebug("Retrieved banner branding asset, size: {Size} bytes", bannerBytes.Length);
            return Task.FromResult(Result<EmailBrandingAsset>.Success(asset));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get banner branding asset");
            return Task.FromResult(Result<EmailBrandingAsset>.Failure($"Failed to get banner: {ex.Message}"));
        }
    }

    /// <summary>
    /// Generates a simple PNG logo placeholder.
    /// In production, replace with actual optimized logo from Azure blob storage.
    /// </summary>
    private static string GenerateLogoBase64()
    {
        // This is a minimal 1x1 transparent PNG as placeholder
        // The actual implementation should load from a resource file or Azure blob
        // For email display, we'll rely on the banner gradient instead
        return "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";
    }

    /// <summary>
    /// Generates a simple banner image matching the LankaConnect gradient colors.
    /// This is a placeholder implementation - in production, use a pre-rendered image.
    /// </summary>
    private static byte[] GenerateBannerImage()
    {
        // Return a minimal 1x1 PNG as placeholder
        // The email template will use CSS background-color as fallback
        return Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");
    }
}
