using LankaConnect.Domain.Business;
using LankaConnect.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Businesses.Queries.GetBusinessImages;

/// <summary>
/// Handler for getting business images
/// </summary>
public sealed class GetBusinessImagesQueryHandler : IRequestHandler<GetBusinessImagesQuery, List<BusinessImageDto>>
{
    private readonly IBusinessRepository _businessRepository;
    private readonly ILogger<GetBusinessImagesQueryHandler> _logger;

    public GetBusinessImagesQueryHandler(
        IBusinessRepository businessRepository,
        ILogger<GetBusinessImagesQueryHandler> logger)
    {
        _businessRepository = businessRepository;
        _logger = logger;
    }

    public async Task<List<BusinessImageDto>> Handle(GetBusinessImagesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting images for business {BusinessId}", request.BusinessId);

            // Get business entity
            var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
            if (business == null)
            {
                _logger.LogWarning("Business not found: {BusinessId}", request.BusinessId);
                return new List<BusinessImageDto>();
            }

            // Convert business images to DTOs, sorted by display order
            var imageDtos = business.GetImagesSortedByDisplayOrder()
                .Select(img => new BusinessImageDto
                {
                    Id = img.Id,
                    OriginalUrl = img.OriginalUrl,
                    ThumbnailUrl = img.ThumbnailUrl,
                    MediumUrl = img.MediumUrl,
                    LargeUrl = img.LargeUrl,
                    AltText = img.AltText,
                    Caption = img.Caption,
                    DisplayOrder = img.DisplayOrder,
                    IsPrimary = img.IsPrimary,
                    FileSizeBytes = img.FileSizeBytes,
                    ContentType = img.ContentType,
                    UploadedAt = img.UploadedAt,
                    Metadata = img.Metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                })
                .ToList();

            _logger.LogInformation("Found {ImageCount} images for business {BusinessId}", 
                imageDtos.Count, request.BusinessId);

            return imageDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting images for business {BusinessId}", request.BusinessId);
            return new List<BusinessImageDto>();
        }
    }
}