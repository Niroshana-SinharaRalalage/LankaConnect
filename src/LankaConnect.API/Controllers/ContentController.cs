using LankaConnect.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LankaConnect.API.Controllers;

/// <summary>
/// Phase 6A.106 Part 3: Generic content management endpoints
/// Handles uploads for rich text editor content (images, etc.) that aren't tied to specific domain entities
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ContentController : ControllerBase
{
    private readonly IImageService _imageService;
    private readonly ILogger<ContentController> _logger;

    public ContentController(
        IImageService imageService,
        ILogger<ContentController> logger)
    {
        _imageService = imageService;
        _logger = logger;
    }

    /// <summary>
    /// Upload an image for use in rich text editors (newsletters, events, etc.)
    /// Phase 6A.106 Part 3: Azure Blob Storage image upload for TipTap editor
    /// </summary>
    /// <param name="image">Image file to upload (max 10MB)</param>
    /// <returns>Image URL for embedding in rich text content</returns>
    /// <response code="200">Image uploaded successfully, returns Azure Blob URL</response>
    /// <response code="400">Invalid image file (size/type/format)</response>
    /// <response code="401">Unauthorized - login required</response>
    [HttpPost("images")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB limit (matches ImageService constraint)
    [ProducesResponseType(typeof(ContentImageUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadImage([FromForm] IFormFile image)
    {
        try
        {
            _logger.LogInformation(
                "Content image upload requested by user {UserId}. FileName: {FileName}, Size: {Size} bytes",
                User.Identity?.Name, image?.FileName, image?.Length);

            // Validate file
            if (image == null || image.Length == 0)
            {
                _logger.LogWarning("Image upload failed: No file provided");
                return BadRequest(new { error = "Image file is required" });
            }

            // Read file data
            using var memoryStream = new MemoryStream();
            await image.CopyToAsync(memoryStream);
            var imageData = memoryStream.ToArray();

            // Use random GUID as business ID since content isn't tied to specific entity
            var businessId = Guid.NewGuid();

            // Upload via ImageService (handles validation + Azure upload)
            var uploadResult = await _imageService.UploadImageAsync(
                imageData,
                image.FileName,
                businessId,
                HttpContext.RequestAborted);

            if (!uploadResult.IsSuccess)
            {
                _logger.LogWarning(
                    "Image upload failed for {FileName}. Errors: {Errors}",
                    image.FileName, string.Join(", ", uploadResult.Errors));
                return BadRequest(new { error = uploadResult.Errors.FirstOrDefault() ?? "Image upload failed" });
            }

            _logger.LogInformation(
                "Image uploaded successfully. URL: {Url}, Size: {Size} bytes",
                uploadResult.Value.Url, uploadResult.Value.SizeBytes);

            return Ok(new ContentImageUploadResponse
            {
                ImageUrl = uploadResult.Value.Url,
                SizeBytes = uploadResult.Value.SizeBytes,
                ContentType = uploadResult.Value.ContentType
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error uploading content image. FileName: {FileName}",
                image?.FileName);
            return StatusCode(500, new { error = "An error occurred while uploading the image" });
        }
    }
}

/// <summary>
/// Response DTO for content image upload
/// </summary>
public record ContentImageUploadResponse
{
    /// <summary>
    /// Azure Blob Storage URL (with SAS token if private container)
    /// </summary>
    public string ImageUrl { get; init; } = string.Empty;

    /// <summary>
    /// Image file size in bytes
    /// </summary>
    public long SizeBytes { get; init; }

    /// <summary>
    /// Image MIME type (e.g., "image/jpeg")
    /// </summary>
    public string ContentType { get; init; } = string.Empty;
}
