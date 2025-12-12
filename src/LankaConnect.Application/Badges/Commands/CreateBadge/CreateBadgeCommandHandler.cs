using LankaConnect.Application.Badges.DTOs;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Badges;
using LankaConnect.Domain.Common;
using MediatR;

namespace LankaConnect.Application.Badges.Commands.CreateBadge;

/// <summary>
/// Handler for CreateBadgeCommand
/// Phase 6A.25: Creates a new badge with uploaded image
/// </summary>
public class CreateBadgeCommandHandler : IRequestHandler<CreateBadgeCommand, Result<BadgeDto>>
{
    private readonly IBadgeRepository _badgeRepository;
    private readonly IAzureBlobStorageService _blobStorageService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateBadgeCommandHandler(
        IBadgeRepository badgeRepository,
        IAzureBlobStorageService blobStorageService,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _badgeRepository = badgeRepository;
        _blobStorageService = blobStorageService;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<BadgeDto>> Handle(CreateBadgeCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate input
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result<BadgeDto>.Failure("Badge name is required");

        if (request.ImageData == null || request.ImageData.Length == 0)
            return Result<BadgeDto>.Failure("Badge image is required");

        if (string.IsNullOrWhiteSpace(request.FileName))
            return Result<BadgeDto>.Failure("File name is required");

        // 2. Check for duplicate name
        var existingBadge = await _badgeRepository.ExistsByNameAsync(request.Name, cancellationToken);
        if (existingBadge)
            return Result<BadgeDto>.Failure($"A badge with the name '{request.Name}' already exists");

        // 3. Get next display order
        var displayOrder = await _badgeRepository.GetNextDisplayOrderAsync(cancellationToken);

        // 4. Upload image to Azure Blob Storage
        var uniqueFileName = $"badges/{Guid.NewGuid()}-{request.FileName}";
        using var stream = new MemoryStream(request.ImageData);

        var (blobName, blobUrl) = await _blobStorageService.UploadFileAsync(
            uniqueFileName,
            stream,
            GetContentType(request.FileName),
            "badges",
            cancellationToken);

        // 5. Create badge entity
        var badgeResult = Badge.Create(
            request.Name,
            blobUrl,
            blobName,
            request.Position,
            displayOrder,
            _currentUserService.UserId);

        if (!badgeResult.IsSuccess)
        {
            // Rollback: Delete uploaded blob if domain validation fails
            await _blobStorageService.DeleteFileAsync(blobName, "badges", cancellationToken);
            return Result<BadgeDto>.Failure(badgeResult.Errors);
        }

        var badge = badgeResult.Value;

        // 6. Save to repository
        await _badgeRepository.AddAsync(badge, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        // 7. Return DTO
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
