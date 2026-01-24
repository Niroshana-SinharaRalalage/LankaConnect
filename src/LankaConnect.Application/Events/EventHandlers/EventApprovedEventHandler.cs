using System.Diagnostics;
using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Users;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

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

        using (LogContext.PushProperty("Operation", "EventApproved"))
        using (LogContext.PushProperty("EntityType", "Event"))
        using (LogContext.PushProperty("EventId", domainEvent.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "EventApproved START: EventId={EventId}, ApprovedAt={ApprovedAt}",
                domainEvent.EventId, domainEvent.ApprovedAt);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Retrieve event data
                var @event = await _eventRepository.GetByIdAsync(domainEvent.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "EventApproved: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        domainEvent.EventId, stopwatch.ElapsedMilliseconds);
                    return;
                }

                // Retrieve organizer's user details
                var organizer = await _userRepository.GetByIdAsync(@event.OrganizerId, cancellationToken);
                if (organizer == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "EventApproved: Organizer not found - OrganizerId={OrganizerId}, Duration={ElapsedMs}ms",
                        @event.OrganizerId, stopwatch.ElapsedMilliseconds);
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
                    "EventApproved: Sending approval email - To={Email}, EventId={EventId}, EventTitle={EventTitle}",
                    organizer.Email.Value, domainEvent.EventId, @event.Title.Value);

                // Phase 6A.75: Use templated email instead of inline HTML
                var result = await _emailService.SendTemplatedEmailAsync(
                    "template-event-approval",
                    organizer.Email.Value,
                    parameters,
                    cancellationToken);

                stopwatch.Stop();

                if (result.IsSuccess)
                {
                    _logger.LogInformation(
                        "EventApproved COMPLETE: Email sent successfully - Email={Email}, EventId={EventId}, Duration={ElapsedMs}ms",
                        organizer.Email.Value, domainEvent.EventId, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogError(
                        "EventApproved FAILED: Email sending failed - Email={Email}, EventId={EventId}, Errors={Errors}, Duration={ElapsedMs}ms",
                        organizer.Email.Value, domainEvent.EventId, string.Join(", ", result.Errors), stopwatch.ElapsedMilliseconds);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "EventApproved CANCELED: Operation was canceled - EventId={EventId}, Duration={ElapsedMs}ms",
                    domainEvent.EventId, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // Fail-silent pattern: Log error but don't throw to prevent transaction rollback
                _logger.LogError(ex,
                    "EventApproved FAILED: Exception occurred - EventId={EventId}, Duration={ElapsedMs}ms",
                    domainEvent.EventId, stopwatch.ElapsedMilliseconds);
            }
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
