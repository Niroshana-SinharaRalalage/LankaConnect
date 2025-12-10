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

            // 2. Set primary image using two-phase approach to avoid unique constraint violation
            // Phase 1: Unmark current primary (if any)
            var unmarkResult = @event.UnmarkCurrentPrimaryImage();
            if (!unmarkResult.IsSuccess)
                return unmarkResult;

            // Save the unmark operation first to ensure database has no primary
            await _unitOfWork.CommitAsync(cancellationToken);

            // Phase 2: Mark new primary
            var markResult = @event.MarkImageAsPrimary(request.ImageId);
            if (!markResult.IsSuccess)
                return markResult;

            // Save the mark operation
            await _unitOfWork.CommitAsync(cancellationToken);

            return Result.Success();
        }
        catch (Exception ex) when (IsUniqueConstraintViolation(ex))
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

    /// <summary>
    /// Checks if the exception (or any inner exception) indicates a unique constraint violation
    /// </summary>
    private static bool IsUniqueConstraintViolation(Exception ex)
    {
        var current = ex;
        while (current != null)
        {
            if (current.Message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase) ||
                current.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) ||
                current.Message.Contains("IX_EventImages_EventId_IsPrimary_True", StringComparison.OrdinalIgnoreCase) ||
                current.Message.Contains("23505", StringComparison.OrdinalIgnoreCase)) // PostgreSQL unique violation error code
            {
                return true;
            }
            current = current.InnerException;
        }
        return false;
    }
}
