using MediatR;

namespace LankaConnect.Application.Businesses.Queries.GetBusinessImages;

/// <summary>
/// Query to get all images for a business
/// </summary>
public sealed record GetBusinessImagesQuery : IRequest<List<BusinessImageDto>>
{
    public Guid BusinessId { get; init; }

    public GetBusinessImagesQuery(Guid businessId)
    {
        BusinessId = businessId;
    }
}

/// <summary>
/// DTO for business image data
/// </summary>
public sealed record BusinessImageDto
{
    public string Id { get; init; } = string.Empty;
    public string OriginalUrl { get; init; } = string.Empty;
    public string ThumbnailUrl { get; init; } = string.Empty;
    public string MediumUrl { get; init; } = string.Empty;
    public string LargeUrl { get; init; } = string.Empty;
    public string AltText { get; init; } = string.Empty;
    public string Caption { get; init; } = string.Empty;
    public int DisplayOrder { get; init; }
    public bool IsPrimary { get; init; }
    public long FileSizeBytes { get; init; }
    public string ContentType { get; init; } = string.Empty;
    public DateTime UploadedAt { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = new();
}