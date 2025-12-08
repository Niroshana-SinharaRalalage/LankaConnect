using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using MediatR;

namespace LankaConnect.Application.Events.Commands.SetPrimaryImage;

/// <summary>
/// Command to set an image as the primary/main thumbnail for an event
/// Phase 6A.13: Primary image selection feature
/// </summary>
public record SetPrimaryImageCommand : IRequest<Result>
{
    public Guid EventId { get; init; }
    public Guid ImageId { get; init; }
}

public class SetPrimaryImageCommandHandler : IRequestHandler<SetPrimaryImageCommand, Result>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetPrimaryImageCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(SetPrimaryImageCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Get event
            var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
            if (@event == null)
                return Result.Failure($"Event with ID {request.EventId} not found");

            // 2. Set primary image (domain logic handles unmarking previous primary)
            var result = @event.SetPrimaryImage(request.ImageId);
            if (!result.IsSuccess)
                return result;

            // 3. Save changes
            await _unitOfWork.CommitAsync(cancellationToken);

            return Result.Success();
        }
        catch (Exception ex) when (ex.Message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase) ||
                                       ex.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) ||
                                       ex.Message.Contains("IX_EventImages_EventId_IsPrimary_True", StringComparison.OrdinalIgnoreCase))
        {
            // Unique constraint violation on primary image index
            // This means there's already another image marked as primary for this event
            return Result.Failure("Failed to set image as primary: only one image per event can be marked as primary. Please ensure the previous primary image was unmarked.");
        }
        catch (Exception ex)
        {
            // Log the full exception for debugging
            return Result.Failure($"An unexpected error occurred: {ex.Message}");
        }
    }
}
