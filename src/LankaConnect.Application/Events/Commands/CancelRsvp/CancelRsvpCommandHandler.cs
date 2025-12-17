using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.Commands.CancelRsvp;

public class CancelRsvpCommandHandler : ICommandHandler<CancelRsvpCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CancelRsvpCommandHandler> _logger;

    public CancelRsvpCommandHandler(
        IEventRepository eventRepository,
        IRegistrationRepository registrationRepository,
        IUnitOfWork unitOfWork,
        ILogger<CancelRsvpCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _registrationRepository = registrationRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(CancelRsvpCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[CancelRsvp] Starting cancellation for EventId={EventId}, UserId={UserId}",
            request.EventId, request.UserId);

        // Verify event exists
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
        {
            _logger.LogWarning("[CancelRsvp] Event not found: EventId={EventId}", request.EventId);
            return Result.Failure("Event not found");
        }

        // Find active registration using GetByEventAndUserAsync (read-only query)
        var registrationReadOnly = await _registrationRepository.GetByEventAndUserAsync(request.EventId, request.UserId, cancellationToken);

        _logger.LogInformation("[CancelRsvp] Registration query result: Found={Found}, Status={Status}",
            registrationReadOnly != null, registrationReadOnly?.Status.ToString() ?? "N/A");

        if (registrationReadOnly == null)
        {
            _logger.LogWarning("[CancelRsvp] No registration found for EventId={EventId}, UserId={UserId}",
                request.EventId, request.UserId);
            return Result.Failure("User is not registered for this event");
        }

        // Check if already cancelled - make operation idempotent (REST best practice)
        // Attempting to cancel an already-cancelled registration should succeed, not error
        if (registrationReadOnly.Status == RegistrationStatus.Cancelled || registrationReadOnly.Status == RegistrationStatus.Refunded)
        {
            _logger.LogInformation("[CancelRsvp] Registration already cancelled/refunded (idempotent operation): Status={Status}", registrationReadOnly.Status);
            // Return success - cancelling an already-cancelled registration is a no-op
            // This follows REST API idempotency best practices: DELETE operations should be idempotent
            return Result.Success();
        }

        // Get the registration WITH tracking so EF Core can save changes
        var registration = await _registrationRepository.GetByIdAsync(registrationReadOnly.Id, cancellationToken);

        if (registration == null)
        {
            _logger.LogError("[CancelRsvp] Failed to retrieve registration with tracking: RegId={RegId}", registrationReadOnly.Id);
            return Result.Failure("Failed to cancel registration");
        }

        // Cancel the registration
        registration.Cancel();

        // Phase 6A.28: Handle sign-up commitments based on user choice
        // Fix: Trust domain model as single source of truth (removed competing deletion strategies)
        if (request.DeleteSignUpCommitments)
        {
            _logger.LogInformation("[CancelRsvp] Deleting commitments via domain model for EventId={EventId}, UserId={UserId}",
                request.EventId, request.UserId);

            var cancelResult = @event.CancelAllUserCommitments(request.UserId);

            if (cancelResult.IsFailure)
            {
                _logger.LogWarning("[CancelRsvp] Failed to delete commitments: {Error}", cancelResult.Error);
            }
            else
            {
                _logger.LogInformation("[CancelRsvp] Commitments cancelled successfully");
            }
        }
        else
        {
            _logger.LogInformation("[CancelRsvp] User chose to keep sign-up commitments for EventId={EventId}, UserId={UserId}",
                request.EventId, request.UserId);
        }

        // Save changes
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation("[CancelRsvp] Cancellation successful for EventId={EventId}, UserId={UserId}",
            request.EventId, request.UserId);

        return Result.Success();
    }
}
