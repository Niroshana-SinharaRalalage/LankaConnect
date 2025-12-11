using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using MediatR;

namespace LankaConnect.Application.Events.Commands.ReplaceEventImage;

/// <summary>
/// Command to replace an existing event image with a new one
/// Maintains the same image ID and display order while updating the content
/// Implements compensating transaction by deleting old blob after successful replacement
/// </summary>
public record ReplaceEventImageCommand : IRequest<Result<EventImage>>
{
    public Guid EventId { get; init; }
    public Guid ImageId { get; init; }
    public byte[] ImageData { get; init; } = Array.Empty<byte>();
    public string FileName { get; init; } = string.Empty;
}

public class ReplaceEventImageCommandHandler : IRequestHandler<ReplaceEventImageCommand, Result<EventImage>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IImageService _imageService;
    private readonly IUnitOfWork _unitOfWork;

    public ReplaceEventImageCommandHandler(
        IEventRepository eventRepository,
        IImageService imageService,
        IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _imageService = imageService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<EventImage>> Handle(ReplaceEventImageCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate image
        var validationResult = _imageService.ValidateImage(request.ImageData, request.FileName);
        if (!validationResult.IsSuccess)
            return Result<EventImage>.Failure(validationResult.Errors);

        // 2. Get event
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
            return Result<EventImage>.Failure($"Event with ID {request.EventId} not found");

        // 3. Store old image URL for cleanup (before replacement)
        var existingImage = @event.Images.FirstOrDefault(i => i.Id == request.ImageId);
        var oldImageUrl = existingImage?.ImageUrl;

        // 4. Upload new image to Azure Blob Storage
        var uploadResult = await _imageService.UploadImageAsync(
            request.ImageData,
            request.FileName,
            request.EventId,
            cancellationToken);

        if (!uploadResult.IsSuccess)
            return Result<EventImage>.Failure(uploadResult.Errors);

        // 5. Replace image in Event aggregate
        var replaceResult = @event.ReplaceImage(request.ImageId, uploadResult.Value.Url, uploadResult.Value.BlobName);
        if (!replaceResult.IsSuccess)
        {
            // Rollback: Delete newly uploaded blob if domain operation fails
            await _imageService.DeleteImageAsync(uploadResult.Value.Url, cancellationToken);
            return Result<EventImage>.Failure(replaceResult.Errors);
        }

        // 6. Save changes
        await _unitOfWork.CommitAsync(cancellationToken);

        // 7. Cleanup: Delete old blob after successful replacement (compensating transaction)
        if (!string.IsNullOrEmpty(oldImageUrl))
        {
            // Fire and forget - don't fail the operation if cleanup fails
            _ = _imageService.DeleteImageAsync(oldImageUrl, cancellationToken);
        }

        return Result<EventImage>.Success(replaceResult.Value);
    }
}
