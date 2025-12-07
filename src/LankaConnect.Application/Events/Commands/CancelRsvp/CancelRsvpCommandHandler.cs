using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.Commands.CancelRsvp;

public class CancelRsvpCommandHandler : ICommandHandler<CancelRsvpCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<CancelRsvpCommandHandler> _logger;

    public CancelRsvpCommandHandler(
        IEventRepository eventRepository,
        IRegistrationRepository registrationRepository,
        IUnitOfWork unitOfWork,
        IApplicationDbContext dbContext,
        ILogger<CancelRsvpCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _registrationRepository = registrationRepository;
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
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

        // Check if already cancelled
        if (registrationReadOnly.Status == RegistrationStatus.Cancelled || registrationReadOnly.Status == RegistrationStatus.Refunded)
        {
            _logger.LogWarning("[CancelRsvp] Registration already cancelled/refunded: Status={Status}", registrationReadOnly.Status);
            return Result.Failure("Registration has already been cancelled");
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

        // Phase 6A.16: Cascade delete sign-up commitments when registration is cancelled
        // Find all sign-up commitments for this user in this event's sign-up lists
        var userCommitments = await _dbContext.SignUpCommitments
            .Where(c => c.UserId == request.UserId && c.SignUpItemId != null)
            .Join(_dbContext.SignUpItems,
                c => c.SignUpItemId,
                item => item.Id,
                (c, item) => new { Commitment = c, Item = item })
            .Join(_dbContext.SignUpLists,
                ci => ci.Item.SignUpListId,
                list => list.Id,
                (ci, list) => new { ci.Commitment, ci.Item, List = list })
            .Where(x => EF.Property<Guid>(x.List, "EventId") == request.EventId)
            .Select(x => x.Commitment)
            .ToListAsync(cancellationToken);

        if (userCommitments.Any())
        {
            _logger.LogInformation("[CancelRsvp] Removing {Count} sign-up commitments for UserId={UserId}, EventId={EventId}",
                userCommitments.Count, request.UserId, request.EventId);

            _dbContext.SignUpCommitments.RemoveRange(userCommitments);
        }

        // Save changes
        await _unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation("[CancelRsvp] Cancellation successful for EventId={EventId}, UserId={UserId}",
            request.EventId, request.UserId);

        return Result.Success();
    }
}
