using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Products.LankaEvents.Application.Commands.UpdateEventOrganizerContact;

public class UpdateEventOrganizerContactCommandHandler : ICommandHandler<UpdateEventOrganizerContactCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateEventOrganizerContactCommandHandler> _logger;

    public UpdateEventOrganizerContactCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateEventOrganizerContactCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateEventOrganizerContactCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "UpdateEventOrganizerContact"))
        using (LogContext.PushProperty("EntityType", "Event"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "UpdateEventOrganizerContact START: EventId={EventId}, PublishContact={PublishContact}, ContactCount={ContactCount}",
                request.EventId, request.PublishOrganizerContact, request.Contacts?.Count ?? 0);

            try
            {
                // Phase 6A.53 FIX: Retrieve event WITH CHANGE TRACKING (trackChanges: true)
                // This is required for EF Core to detect changes when we modify the entity
                var @event = await _eventRepository.GetByIdAsync(request.EventId, trackChanges: true, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateEventOrganizerContact FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Event not found");
                }

                _logger.LogInformation(
                    "UpdateEventOrganizerContact: Event loaded - EventId={EventId}, Title={Title}, CurrentPublishContact={CurrentPublishContact}",
                    @event.Id, @event.Title.Value, @event.PublishOrganizerContact);

                // Call domain method to set organizer contacts
                var contacts = (request.Contacts ?? new List<OrganizerContactRequest>())
                    .Select(c => (c.ContactName, c.ContactEmail, c.ContactPhone, c.LinkedUserId, c.IsPrimary))
                    .ToList();

                var setContactResult = @event.SetOrganizerContacts(
                    request.PublishOrganizerContact,
                    contacts
                );

                if (setContactResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateEventOrganizerContact FAILED: Domain validation failed - EventId={EventId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, setContactResult.Error, stopwatch.ElapsedMilliseconds);

                    return setContactResult;
                }

                _logger.LogInformation(
                    "UpdateEventOrganizerContact: Domain method succeeded - EventId={EventId}, PublishContact={PublishContact}",
                    @event.Id, @event.PublishOrganizerContact);

                // Save changes (EF Core tracks changes automatically)
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "UpdateEventOrganizerContact COMPLETE: EventId={EventId}, PublishContact={PublishContact}, Duration={ElapsedMs}ms",
                    request.EventId, request.PublishOrganizerContact, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "UpdateEventOrganizerContact FAILED: Exception occurred - EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw; // Re-throw to let MediatR/API handle
            }
        }
    }
}
