using LankaConnect.Modules.Identity.Contracts; // W4.6.a: ICurrentUserService moved here
using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Commands.UnlinkOrganizerContactUser;

/// <summary>
/// Phase 6A.133: Handles unlinking a user from an organizer contact.
/// Removes co-organizer access. Contact info remains.
/// </summary>
public class UnlinkOrganizerContactUserCommandHandler : ICommandHandler<UnlinkOrganizerContactUserCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UnlinkOrganizerContactUserCommandHandler> _logger;

    public UnlinkOrganizerContactUserCommandHandler(
        IEventRepository eventRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<UnlinkOrganizerContactUserCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(UnlinkOrganizerContactUserCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "UnlinkOrganizerContactUser"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                if (request.EventId == Guid.Empty)
                    return Result.Failure("Event ID is required");

                if (request.ContactId == Guid.Empty)
                    return Result.Failure("Contact ID is required");

                _logger.LogInformation(
                    "UnlinkOrganizerContactUser START: EventId={EventId}, ContactId={ContactId}",
                    request.EventId, request.ContactId);

                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                    return Result.Failure("Event not found");

                // Authorization: Only organizers can unlink
                if (!@event.IsOrganizer(_currentUserService.UserId) && !_currentUserService.IsAdmin)
                {
                    _logger.LogWarning(
                        "UnlinkOrganizerContactUser FORBIDDEN: User {UserId} is not an organizer of Event {EventId}",
                        _currentUserService.UserId, request.EventId);
                    return Result.Failure("Only event organizers can unlink co-organizers");
                }

                var unlinkResult = @event.UnlinkOrganizerContact(request.ContactId);
                if (unlinkResult.IsFailure)
                    return unlinkResult;

                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "UnlinkOrganizerContactUser COMPLETE: EventId={EventId}, ContactId={ContactId}, Duration={ElapsedMs}ms",
                    request.EventId, request.ContactId, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "UnlinkOrganizerContactUser FAILED: EventId={EventId}, ContactId={ContactId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, request.ContactId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
