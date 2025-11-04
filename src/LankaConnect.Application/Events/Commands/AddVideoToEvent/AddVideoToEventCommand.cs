using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using MediatR;

namespace LankaConnect.Application.Events.Commands.AddVideoToEvent;

/// <summary>
/// Command to add a video to an event's gallery
/// Uploads video and thumbnail to Azure Blob Storage and adds metadata to Event aggregate
/// </summary>
public record AddVideoToEventCommand : IRequest<Result<EventVideo>>
{
    public Guid EventId { get; init; }
    public byte[] VideoData { get; init; } = Array.Empty<byte>();
    public string VideoFileName { get; init; } = string.Empty;
    public byte[] ThumbnailData { get; init; } = Array.Empty<byte>();
    public string ThumbnailFileName { get; init; } = string.Empty;
    public TimeSpan? Duration { get; init; }
    public string Format { get; init; } = string.Empty;
}

public class AddVideoToEventCommandHandler : IRequestHandler<AddVideoToEventCommand, Result<EventVideo>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IImageService _imageService; // Reusing for video/thumbnail uploads
    private readonly IUnitOfWork _unitOfWork;

    public AddVideoToEventCommandHandler(
        IEventRepository eventRepository,
        IImageService imageService,
        IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _imageService = imageService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<EventVideo>> Handle(AddVideoToEventCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate video
        var videoValidation = _imageService.ValidateImage(request.VideoData, request.VideoFileName);
        if (!videoValidation.IsSuccess)
            return Result<EventVideo>.Failure(videoValidation.Errors);

        // 2. Validate thumbnail
        var thumbnailValidation = _imageService.ValidateImage(request.ThumbnailData, request.ThumbnailFileName);
        if (!thumbnailValidation.IsSuccess)
            return Result<EventVideo>.Failure(thumbnailValidation.Errors);

        // 3. Get event
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
            return Result<EventVideo>.Failure($"Event with ID {request.EventId} not found");

        // 4. Upload video to Azure Blob Storage
        var videoUploadResult = await _imageService.UploadImageAsync(
            request.VideoData,
            request.VideoFileName,
            request.EventId,
            cancellationToken);

        if (!videoUploadResult.IsSuccess)
            return Result<EventVideo>.Failure(videoUploadResult.Errors);

        // 5. Upload thumbnail to Azure Blob Storage
        var thumbnailUploadResult = await _imageService.UploadImageAsync(
            request.ThumbnailData,
            request.ThumbnailFileName,
            request.EventId,
            cancellationToken);

        if (!thumbnailUploadResult.IsSuccess)
        {
            // Rollback: Delete video blob if thumbnail upload fails
            await _imageService.DeleteImageAsync(videoUploadResult.Value.Url, cancellationToken);
            return Result<EventVideo>.Failure(thumbnailUploadResult.Errors);
        }

        // 6. Add video metadata to Event aggregate
        var addVideoResult = @event.AddVideo(
            videoUploadResult.Value.Url,
            videoUploadResult.Value.BlobName,
            thumbnailUploadResult.Value.Url,
            thumbnailUploadResult.Value.BlobName,
            request.Duration,
            request.Format,
            videoUploadResult.Value.SizeBytes);

        if (!addVideoResult.IsSuccess)
        {
            // Rollback: Delete both uploaded blobs if domain operation fails
            await _imageService.DeleteImageAsync(videoUploadResult.Value.Url, cancellationToken);
            await _imageService.DeleteImageAsync(thumbnailUploadResult.Value.Url, cancellationToken);
            return Result<EventVideo>.Failure(addVideoResult.Errors);
        }

        // 7. Save changes
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result<EventVideo>.Success(addVideoResult.Value);
    }
}
