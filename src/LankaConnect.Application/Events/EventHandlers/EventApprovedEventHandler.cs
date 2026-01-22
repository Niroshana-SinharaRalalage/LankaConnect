using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Users;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Phase 6A.75: Handles EventApprovedEvent to send approval notification email to event organizer.
/// Uses database-backed email template for consistent branding.
/// </summary>
public class EventApprovedEventHandler : INotificationHandler<DomainEventNotification<EventApprovedEvent>>
{
    private readonly IEmailService _emailService;
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IEmailUrlHelper _emailUrlHelper;
    private readonly ILogger<EventApprovedEventHandler> _logger;

    public EventApprovedEventHandler(
        IEmailService emailService,
        IUserRepository userRepository,
        IEventRepository eventRepository,
        IEmailUrlHelper emailUrlHelper,
        ILogger<EventApprovedEventHandler> logger)
    {
        _emailService = emailService;
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _emailUrlHelper = emailUrlHelper;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<EventApprovedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        _logger.LogInformation(
            "[Phase 6A.75] Processing EventApprovedEvent for Event {EventId}",
            domainEvent.EventId);

        try
        {
            // Retrieve event data
            var @event = await _eventRepository.GetByIdAsync(domainEvent.EventId, cancellationToken);
            if (@event == null)
            {
                _logger.LogWarning(
                    "[Phase 6A.75] Event {EventId} not found for EventApprovedEvent, skipping email",
                    domainEvent.EventId);
                return;
            }

            // Retrieve organizer's user details
            var organizer = await _userRepository.GetByIdAsync(@event.OrganizerId, cancellationToken);
            if (organizer == null)
            {
                _logger.LogWarning(
                    "[Phase 6A.75] Organizer {OrganizerId} not found for EventApprovedEvent, skipping email",
                    @event.OrganizerId);
                return;
            }

            var organizerName = $"{organizer.FirstName} {organizer.LastName}";

            // Phase 6A.75: Build URLs using centralized URL helper
            var eventUrl = _emailUrlHelper.BuildEventDetailsUrl(@event.Id);
            var eventManageUrl = _emailUrlHelper.BuildEventManageUrl(@event.Id);

            // Build template parameters
            var parameters = new Dictionary<string, object>
            {
                { "OrganizerName", organizerName },
                { "EventTitle", @event.Title.Value },
                { "EventStartDate", @event.StartDate.ToString("MMMM dd, yyyy") },
                { "EventStartTime", @event.StartDate.ToString("h:mm tt") },
                { "EventLocation", GetEventLocationString(@event) },
                { "ApprovedAt", domainEvent.ApprovedAt.ToString("MMMM dd, yyyy h:mm tt") },
                { "EventUrl", eventUrl },
                { "EventManageUrl", eventManageUrl }
            };

            _logger.LogInformation(
                "[Phase 6A.75] Sending event approval email to {Email} for Event {EventId} ({EventTitle})",
                organizer.Email.Value, domainEvent.EventId, @event.Title.Value);

            // Phase 6A.75: Use templated email instead of inline HTML
            var result = await _emailService.SendTemplatedEmailAsync(
                "template-event-approval",
                organizer.Email.Value,
                parameters,
                cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "[Phase 6A.75] Event approval email sent successfully to {Email} for Event {EventId}",
                    organizer.Email.Value, domainEvent.EventId);
            }
            else
            {
                _logger.LogError(
                    "[Phase 6A.75] Failed to send event approval email to {Email} for Event {EventId}: {Errors}",
                    organizer.Email.Value, domainEvent.EventId, string.Join(", ", result.Errors));
            }
        }
        catch (Exception ex)
        {
            // Fail-silent pattern: Log error but don't throw to prevent transaction rollback
            _logger.LogError(ex,
                "[Phase 6A.75] Error handling EventApprovedEvent for Event {EventId}",
                domainEvent.EventId);
        }
    }

    /// <summary>
    /// Safely extracts event location string with defensive null handling.
    /// </summary>
    private static string GetEventLocationString(Event @event)
    {
        if (@event.Location?.Address == null)
            return "Online Event";

        var street = @event.Location.Address.Street;
        var city = @event.Location.Address.City;
        var state = @event.Location.Address.State;

        if (string.IsNullOrWhiteSpace(street) && string.IsNullOrWhiteSpace(city))
            return "Online Event";

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(street)) parts.Add(street);
        if (!string.IsNullOrWhiteSpace(city)) parts.Add(city);
        if (!string.IsNullOrWhiteSpace(state)) parts.Add(state);

        return string.Join(", ", parts);
    }
}
