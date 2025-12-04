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
    private readonly IImageService _imageService; // For thumbnail uploads
    private readonly IAzureBlobStorageService _blobStorageService; // Direct access for video uploads
    private readonly IUnitOfWork _unitOfWork;

    public AddVideoToEventCommandHandler(
        IEventRepository eventRepository,
        IImageService imageService,
        IAzureBlobStorageService blobStorageService,
        IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _imageService = imageService;
        _blobStorageService = blobStorageService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<EventVideo>> Handle(AddVideoToEventCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate video (basic checks - frontend already validated format/size)
        if (request.VideoData == null || request.VideoData.Length == 0)
            return Result<EventVideo>.Failure("Video file cannot be empty");

        if (request.VideoData.Length > 100 * 1024 * 1024) // 100MB
            return Result<EventVideo>.Failure("Video file size exceeds maximum allowed size of 100 MB");

        // 2. Validate thumbnail (use ImageService for image validation)
        var thumbnailValidation = _imageService.ValidateImage(request.ThumbnailData, request.ThumbnailFileName);
        if (!thumbnailValidation.IsSuccess)
            return Result<EventVideo>.Failure(thumbnailValidation.Errors);

        // 3. Get event
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
            return Result<EventVideo>.Failure($"Event with ID {request.EventId} not found");

        // 4. Upload video directly to Azure Blob Storage (bypass ImageService validation)
        var videoContentType = GetVideoContentType(request.VideoFileName);
        using var videoStream = new MemoryStream(request.VideoData);
        var (videoBlobName, videoUrl) = await _blobStorageService.UploadFileAsync(
            request.VideoFileName,
            videoStream,
            videoContentType,
            cancellationToken: cancellationToken);

        // 5. Upload thumbnail using ImageService
        var thumbnailUploadResult = await _imageService.UploadImageAsync(
            request.ThumbnailData,
            request.ThumbnailFileName,
            request.EventId,
            cancellationToken);

        if (!thumbnailUploadResult.IsSuccess)
        {
            // Rollback: Delete video blob if thumbnail upload fails
            await _blobStorageService.DeleteFileAsync(videoBlobName, containerName: null, cancellationToken);
            return Result<EventVideo>.Failure(thumbnailUploadResult.Errors);
        }

        // 6. Add video metadata to Event aggregate
        var addVideoResult = @event.AddVideo(
            videoUrl,
            videoBlobName,
            thumbnailUploadResult.Value.Url,
            thumbnailUploadResult.Value.BlobName,
            request.Duration,
            request.Format,
            request.VideoData.Length);

        if (!addVideoResult.IsSuccess)
        {
            // Rollback: Delete both uploaded blobs if domain operation fails
            await _blobStorageService.DeleteFileAsync(videoBlobName, containerName: null, cancellationToken);
            await _imageService.DeleteImageAsync(thumbnailUploadResult.Value.Url, cancellationToken);
            return Result<EventVideo>.Failure(addVideoResult.Errors);
        }

        // 7. Save changes
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result<EventVideo>.Success(addVideoResult.Value);
    }

    private static string GetVideoContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".ogg" => "video/ogg",
            ".mov" => "video/quicktime",
            _ => "application/octet-stream"
        };
    }
}
