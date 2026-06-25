using LankaConnect.Modules.Identity.Contracts; // W4.6.a: ICurrentUserService moved here
using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Common.Options;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users.DomainEvents; // W4.7.a: user-aggregate events moved here
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.BatchLinkOrganizerContacts;

/// <summary>
/// Phase 6A.133: Handles batch linking of registered users to organizer contacts.
/// Validates all users exist, checks max co-organizer limit, then links.
/// </summary>
public class BatchLinkOrganizerContactsCommandHandler : ICommandHandler<BatchLinkOrganizerContactsCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IIdentityQueries _identityQueries;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly EventSettings _eventSettings;
    private readonly ILogger<BatchLinkOrganizerContactsCommandHandler> _logger;

    public BatchLinkOrganizerContactsCommandHandler(
        IEventRepository eventRepository,
        IIdentityQueries identityQueries,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        IOptions<EventSettings> eventSettings,
        ILogger<BatchLinkOrganizerContactsCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _identityQueries = identityQueries;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _eventSettings = eventSettings.Value;
        _logger = logger;
    }

    public async Task<Result> Handle(BatchLinkOrganizerContactsCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "BatchLinkOrganizerContacts"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // 1. Validate request
                if (request.EventId == Guid.Empty)
                    return Result.Failure("Event ID is required");

                if (request.Links == null || request.Links.Count == 0)
                    return Result.Failure("At least one link is required");

                _logger.LogInformation(
                    "BatchLinkOrganizerContacts START: EventId={EventId}, LinkCount={LinkCount}",
                    request.EventId, request.Links.Count);

                // 2. Load event
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                    return Result.Failure("Event not found");

                // 3. Authorization: Only organizers (primary or co-organizer) can link
                if (!@event.IsOrganizer(_currentUserService.UserId) && !_currentUserService.IsAdmin)
                {
                    _logger.LogWarning(
                        "BatchLinkOrganizerContacts FORBIDDEN: User {UserId} is not an organizer of Event {EventId}",
                        _currentUserService.UserId, request.EventId);
                    return Result.Failure("Only event organizers can link co-organizers");
                }

                // 4. Check max co-organizer limit
                var currentLinkedCount = @event.OrganizerContacts.Count(c => c.LinkedUserId.HasValue);
                var newLinkCount = request.Links.Count;
                if (currentLinkedCount + newLinkCount > _eventSettings.MaxCoOrganizersPerEvent)
                {
                    return Result.Failure(
                        $"Cannot link {newLinkCount} co-organizers. " +
                        $"Current: {currentLinkedCount}, Maximum: {_eventSettings.MaxCoOrganizersPerEvent}");
                }

                // 5. Validate all users exist
                foreach (var link in request.Links)
                {
                    var user = await _identityQueries.GetContactInfoAsync(link.UserId, cancellationToken);
                    if (user == null)
                    {
                        return Result.Failure($"User with ID {link.UserId} not found");
                    }

                    if (!user.IsActive)
                    {
                        return Result.Failure($"User '{user.DisplayName}' is not active");
                    }
                }

                // 6. Batch link via domain method (handles all business rule validation)
                var linkTuples = request.Links
                    .Select(l => (l.ContactId, l.UserId))
                    .ToList();

                var linkResult = @event.BatchLinkOrganizerContacts(linkTuples);
                if (linkResult.IsFailure)
                    return linkResult;

                // 7. Persist
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "BatchLinkOrganizerContacts COMPLETE: EventId={EventId}, LinkedCount={LinkedCount}, Duration={ElapsedMs}ms",
                    request.EventId, request.Links.Count, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "BatchLinkOrganizerContacts FAILED: EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
