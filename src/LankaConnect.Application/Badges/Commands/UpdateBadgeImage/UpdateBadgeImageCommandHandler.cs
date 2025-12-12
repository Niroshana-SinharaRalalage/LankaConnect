using LankaConnect.Application.Badges.DTOs;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Badges;
using LankaConnect.Domain.Common;
using MediatR;

namespace LankaConnect.Application.Badges.Commands.UpdateBadgeImage;

/// <summary>
/// Handler for UpdateBadgeImageCommand
/// Phase 6A.25: Updates a badge's image (uploads new image and deletes old one)
/// </summary>
public class UpdateBadgeImageCommandHandler : IRequestHandler<UpdateBadgeImageCommand, Result<BadgeDto>>
{
    private readonly IBadgeRepository _badgeRepository;
    private readonly IAzureBlobStorageService _blobStorageService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateBadgeImageCommandHandler(
        IBadgeRepository badgeRepository,
        IAzureBlobStorageService blobStorageService,
        IUnitOfWork unitOfWork)
    {
        _badgeRepository = badgeRepository;
        _blobStorageService = blobStorageService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<BadgeDto>> Handle(UpdateBadgeImageCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate input
        if (request.ImageData == null || request.ImageData.Length == 0)
            return Result<BadgeDto>.Failure("Badge image is required");

        if (string.IsNullOrWhiteSpace(request.FileName))
            return Result<BadgeDto>.Failure("File name is required");

        // 2. Get existing badge
        var badge = await _badgeRepository.GetByIdAsync(request.BadgeId, cancellationToken);
        if (badge == null)
            return Result<BadgeDto>.Failure($"Badge with ID {request.BadgeId} not found");

        // 3. Store old blob name for deletion
        var oldBlobName = badge.BlobName;

        // 4. Upload new image to Azure Blob Storage
        var uniqueFileName = $"badges/{Guid.NewGuid()}-{request.FileName}";
        using var stream = new MemoryStream(request.ImageData);

        var (blobName, blobUrl) = await _blobStorageService.UploadFileAsync(
            uniqueFileName,
            stream,
            GetContentType(request.FileName),
            "badges",
            cancellationToken);

        // 5. Update badge with new image
        var updateResult = badge.UpdateImage(blobUrl, blobName);
        if (!updateResult.IsSuccess)
        {
            // Rollback: Delete the newly uploaded blob
            await _blobStorageService.DeleteFileAsync(blobName, "badges", cancellationToken);
            return Result<BadgeDto>.Failure(updateResult.Errors);
        }

        // 6. Save changes
        _badgeRepository.Update(badge);
        await _unitOfWork.CommitAsync(cancellationToken);

        // 7. Delete old image (non-blocking, ignore errors)
        try
        {
            await _blobStorageService.DeleteFileAsync(oldBlobName, "badges", cancellationToken);
        }
        catch
        {
            // Log but don't fail - old image cleanup is best effort
        }

        // 8. Return updated DTO
        var dto = new BadgeDto
        {
            Id = badge.Id,
            Name = badge.Name,
            ImageUrl = badge.ImageUrl,
            Position = badge.Position,
            IsActive = badge.IsActive,
            IsSystem = badge.IsSystem,
            DisplayOrder = badge.DisplayOrder,
            CreatedAt = badge.CreatedAt
        };

        return Result<BadgeDto>.Success(dto);
    }

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            _ => "application/octet-stream"
        };
    }
}
