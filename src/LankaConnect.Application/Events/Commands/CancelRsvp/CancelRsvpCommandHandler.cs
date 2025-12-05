using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.Commands.CancelRsvp;

public class CancelRsvpCommandHandler : ICommandHandler<CancelRsvpCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CancelRsvpCommandHandler> _logger;

    public CancelRsvpCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<CancelRsvpCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(CancelRsvpCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[CancelRsvp] Starting cancellation for EventId={EventId}, UserId={UserId}",
            request.EventId, request.UserId);

        // Retrieve event
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
        {
            _logger.LogWarning("[CancelRsvp] Event not found: EventId={EventId}", request.EventId);
            return Result.Failure("Event not found");
        }

        _logger.LogInformation("[CancelRsvp] Event found. Total registrations in collection: {Count}",
            @event.Registrations.Count);

        // Log each registration for debugging
        foreach (var reg in @event.Registrations)
        {
            _logger.LogInformation("[CancelRsvp] Registration found: RegId={RegId}, UserId={UserId}, Status={Status}",
                reg.Id, reg.UserId, reg.Status);
        }

        // Use domain method to cancel user registration
        var cancelResult = @event.CancelRegistration(request.UserId);
        if (cancelResult.IsFailure)
        {
            _logger.LogError("[CancelRsvp] Cancellation failed: {Error}", cancelResult.Error);
            return cancelResult;
        }

        // Save changes (EF Core tracks changes automatically)
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation("[CancelRsvp] Cancellation successful for EventId={EventId}, UserId={UserId}",
            request.EventId, request.UserId);

        return Result.Success();
    }
}
