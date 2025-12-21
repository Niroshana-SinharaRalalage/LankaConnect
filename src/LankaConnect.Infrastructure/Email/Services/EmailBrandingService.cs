using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Infrastructure.Email.Services;

/// <summary>
/// Phase 6A.35/6A.37: Implementation of email branding service for providing logo and banner images.
/// Images are embedded inline in emails using CID (Content-ID) to ensure they display
/// immediately without requiring users to click "display images" in email clients.
///
/// Phase 6A.37: Updated to fetch actual banner images from Azure Blob Storage.
/// Banners match the LankaConnect landing page design with gradient and cross pattern.
/// </summary>
public class EmailBrandingService : IEmailBrandingService
{
    private readonly ILogger<EmailBrandingService> _logger;
    private readonly IMemoryCache _cache;
    private readonly HttpClient _httpClient;

    // Content-IDs for CID embedding - these are referenced in email templates using src="cid:{ContentId}"
    private const string LogoContentId = "lankaconnect-logo";
    private const string BannerContentId = "lankaconnect-banner";
    private const string HeaderBannerContentId = "email-header-banner";
    private const string FooterBannerContentId = "email-footer-banner";

    // Azure Blob Storage URLs for banner images
    private const string HeaderBannerUrl = "https://lankaconnectstrgaccount.blob.core.windows.net/business-images/email-assets/email-header-banner.png";
    private const string FooterBannerUrl = "https://lankaconnectstrgaccount.blob.core.windows.net/business-images/email-assets/email-footer-banner.png";

    // Cache duration for banner images (1 hour)
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    public EmailBrandingService(
        ILogger<EmailBrandingService> logger,
        IMemoryCache cache,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _cache = cache;
        _httpClient = httpClientFactory.CreateClient();
    }

    public Task<Result<EmailBrandingAsset>> GetLogoAsync(CancellationToken cancellationToken = default)
    {
        // Logo is now part of the footer banner, return minimal placeholder
        try
        {
            var logoBytes = Convert.FromBase64String(MinimalPngBase64);

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
        // Redirect to GetHeaderBannerAsync for backwards compatibility
        return GetHeaderBannerAsync(cancellationToken);
    }

    public async Task<Result<EmailBrandingAsset>> GetHeaderBannerAsync(CancellationToken cancellationToken = default)
    {
        const string cacheKey = "email-branding-header-banner";

        try
        {
            // Try to get from cache first
            if (_cache.TryGetValue(cacheKey, out byte[]? cachedBytes) && cachedBytes != null)
            {
                _logger.LogDebug("Retrieved header banner from cache, size: {Size} bytes", cachedBytes.Length);
                return Result<EmailBrandingAsset>.Success(new EmailBrandingAsset
                {
                    Content = cachedBytes,
                    ContentType = "image/png",
                    FileName = "email-header-banner.png",
                    ContentId = HeaderBannerContentId
                });
            }

            // Download from Azure Blob Storage
            var imageBytes = await DownloadImageBytesAsync(HeaderBannerUrl, cancellationToken);
            if (imageBytes == null || imageBytes.Length == 0)
            {
                _logger.LogWarning("Failed to download header banner from {Url}", HeaderBannerUrl);
                return Result<EmailBrandingAsset>.Failure("Failed to download header banner");
            }

            // Cache the image
            _cache.Set(cacheKey, imageBytes, CacheDuration);

            _logger.LogDebug("Downloaded and cached header banner, size: {Size} bytes", imageBytes.Length);

            return Result<EmailBrandingAsset>.Success(new EmailBrandingAsset
            {
                Content = imageBytes,
                ContentType = "image/png",
                FileName = "email-header-banner.png",
                ContentId = HeaderBannerContentId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get header banner branding asset");
            return Result<EmailBrandingAsset>.Failure($"Failed to get header banner: {ex.Message}");
        }
    }

    public async Task<Result<EmailBrandingAsset>> GetFooterBannerAsync(CancellationToken cancellationToken = default)
    {
        const string cacheKey = "email-branding-footer-banner";

        try
        {
            // Try to get from cache first
            if (_cache.TryGetValue(cacheKey, out byte[]? cachedBytes) && cachedBytes != null)
            {
                _logger.LogDebug("Retrieved footer banner from cache, size: {Size} bytes", cachedBytes.Length);
                return Result<EmailBrandingAsset>.Success(new EmailBrandingAsset
                {
                    Content = cachedBytes,
                    ContentType = "image/png",
                    FileName = "email-footer-banner.png",
                    ContentId = FooterBannerContentId
                });
            }

            // Download from Azure Blob Storage
            var imageBytes = await DownloadImageBytesAsync(FooterBannerUrl, cancellationToken);
            if (imageBytes == null || imageBytes.Length == 0)
            {
                _logger.LogWarning("Failed to download footer banner from {Url}", FooterBannerUrl);
                return Result<EmailBrandingAsset>.Failure("Failed to download footer banner");
            }

            // Cache the image
            _cache.Set(cacheKey, imageBytes, CacheDuration);

            _logger.LogDebug("Downloaded and cached footer banner, size: {Size} bytes", imageBytes.Length);

            return Result<EmailBrandingAsset>.Success(new EmailBrandingAsset
            {
                Content = imageBytes,
                ContentType = "image/png",
                FileName = "email-footer-banner.png",
                ContentId = FooterBannerContentId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get footer banner branding asset");
            return Result<EmailBrandingAsset>.Failure($"Failed to get footer banner: {ex.Message}");
        }
    }

    public async Task<Result<EmailBrandingAsset>> DownloadImageAsync(
        string imageUrl,
        string contentId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return Result<EmailBrandingAsset>.Failure("Image URL is required");
        }

        try
        {
            // Use URL as cache key
            var cacheKey = $"email-image-{contentId}-{imageUrl.GetHashCode()}";

            // Try to get from cache first
            if (_cache.TryGetValue(cacheKey, out byte[]? cachedBytes) && cachedBytes != null)
            {
                _logger.LogDebug("Retrieved image from cache for {ContentId}, size: {Size} bytes", contentId, cachedBytes.Length);
                return Result<EmailBrandingAsset>.Success(CreateAssetFromBytes(cachedBytes, imageUrl, contentId));
            }

            // Download the image
            var imageBytes = await DownloadImageBytesAsync(imageUrl, cancellationToken);
            if (imageBytes == null || imageBytes.Length == 0)
            {
                _logger.LogWarning("Failed to download image from {Url}", imageUrl);
                return Result<EmailBrandingAsset>.Failure($"Failed to download image from {imageUrl}");
            }

            // Cache with shorter duration for dynamic images (15 minutes)
            _cache.Set(cacheKey, imageBytes, TimeSpan.FromMinutes(15));

            _logger.LogDebug("Downloaded and cached image from {Url}, size: {Size} bytes", imageUrl, imageBytes.Length);

            return Result<EmailBrandingAsset>.Success(CreateAssetFromBytes(imageBytes, imageUrl, contentId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download image from {Url}", imageUrl);
            return Result<EmailBrandingAsset>.Failure($"Failed to download image: {ex.Message}");
        }
    }

    private async Task<byte[]?> DownloadImageBytesAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to download image: {StatusCode} from {Url}", response.StatusCode, url);
                return null;
            }

            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading image from {Url}", url);
            return null;
        }
    }

    private static EmailBrandingAsset CreateAssetFromBytes(byte[] bytes, string url, string contentId)
    {
        // Determine content type from URL extension
        var contentType = "image/png";
        if (url.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
            url.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
        {
            contentType = "image/jpeg";
        }
        else if (url.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
        {
            contentType = "image/webp";
        }
        else if (url.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
        {
            contentType = "image/gif";
        }

        // Extract filename from URL
        var uri = new Uri(url);
        var fileName = Path.GetFileName(uri.LocalPath);
        if (string.IsNullOrEmpty(fileName))
        {
            fileName = $"{contentId}.png";
        }

        return new EmailBrandingAsset
        {
            Content = bytes,
            ContentType = contentType,
            FileName = fileName,
            ContentId = contentId
        };
    }

    // Minimal 1x1 transparent PNG as fallback
    private const string MinimalPngBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";
}
