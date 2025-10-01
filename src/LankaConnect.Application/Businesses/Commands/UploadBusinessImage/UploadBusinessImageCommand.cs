using LankaConnect.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace LankaConnect.Application.Businesses.Commands.UploadBusinessImage;

/// <summary>
/// Command to upload an image for a business
/// </summary>
public sealed record UploadBusinessImageCommand : IRequest<Result<UploadBusinessImageResponse>>
{
    public Guid BusinessId { get; init; }
    public IFormFile Image { get; init; } = null!;
    public string? AltText { get; init; }
    public string? Caption { get; init; }
    public bool IsPrimary { get; init; }
    public int DisplayOrder { get; init; }
}

/// <summary>
/// Response containing uploaded image details
/// </summary>
public sealed record UploadBusinessImageResponse
{
    public string ImageId { get; init; } = string.Empty;
    public string OriginalUrl { get; init; } = string.Empty;
    public string ThumbnailUrl { get; init; } = string.Empty;
    public string MediumUrl { get; init; } = string.Empty;
    public string LargeUrl { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public DateTime UploadedAt { get; init; }
}