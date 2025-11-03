using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using MediatR;

namespace LankaConnect.Application.Events.Commands.AddImageToEvent;

/// <summary>
/// Command to add an image to an event's gallery
/// Uploads image to Azure Blob Storage and adds metadata to Event aggregate
/// </summary>
public record AddImageToEventCommand : IRequest<Result<EventImage>>
{
    public Guid EventId { get; init; }
    public byte[] ImageData { get; init; } = Array.Empty<byte>();
    public string FileName { get; init; } = string.Empty;
}

public class AddImageToEventCommandHandler : IRequestHandler<AddImageToEventCommand, Result<EventImage>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IImageService _imageService;
    private readonly IUnitOfWork _unitOfWork;

    public AddImageToEventCommandHandler(
        IEventRepository eventRepository,
        IImageService _imageService,
        IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        this._imageService = _imageService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<EventImage>> Handle(AddImageToEventCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate image
        var validationResult = _imageService.ValidateImage(request.ImageData, request.FileName);
        if (!validationResult.IsSuccess)
            return Result<EventImage>.Failure(validationResult.Errors);

        // 2. Get event
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
            return Result<EventImage>.Failure($"Event with ID {request.EventId} not found");

        // 3. Upload image to Azure Blob Storage
        var uploadResult = await _imageService.UploadImageAsync(
            request.ImageData,
            request.FileName,
            request.EventId, // Use EventId as businessId for organizing images
            cancellationToken);

        if (!uploadResult.IsSuccess)
            return Result<EventImage>.Failure(uploadResult.Errors);

        // 4. Add image metadata to Event aggregate
        var addImageResult = @event.AddImage(uploadResult.Value.Url, uploadResult.Value.BlobName);
        if (!addImageResult.IsSuccess)
        {
            // Rollback: Delete uploaded blob if domain operation fails
            await _imageService.DeleteImageAsync(uploadResult.Value.Url, cancellationToken);
            return Result<EventImage>.Failure(addImageResult.Errors);
        }

        // 5. Save changes
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result<EventImage>.Success(addImageResult.Value);
    }
}
