using System.Diagnostics;
using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Constants;
using LankaConnect.Application.Common.Helpers;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Users;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Handles RegistrationCancelledEvent to send cancellation confirmation email to attendee
/// Phase 6A.87: Migrated to ITypedEmailService for hybrid email support
/// </summary>
public class RegistrationCancelledEventHandler : INotificationHandler<DomainEventNotification<RegistrationCancelledEvent>>
{
    private readonly IEmailService _emailService;
    private readonly ITypedEmailService _typedEmailService;  // Phase 6A.87: Typed email service
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IEmailUrlHelper _emailUrlHelper;  // Phase 6A.83: Added for EventDetailsUrl
    private readonly ILogger<RegistrationCancelledEventHandler> _logger;

    private const string HandlerName = nameof(RegistrationCancelledEventHandler);  // Phase 6A.87: For feature flag lookup

    public RegistrationCancelledEventHandler(
        IEmailService emailService,
        ITypedEmailService typedEmailService,  // Phase 6A.87: Typed email service
        IUserRepository userRepository,
        IEventRepository eventRepository,
        IEmailUrlHelper emailUrlHelper,  // Phase 6A.83: Added for EventDetailsUrl
        ILogger<RegistrationCancelledEventHandler> logger)
    {
        _emailService = emailService;
        _typedEmailService = typedEmailService;  // Phase 6A.87: Typed email service
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _emailUrlHelper = emailUrlHelper;  // Phase 6A.83: Added for EventDetailsUrl
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<RegistrationCancelledEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        using (LogContext.PushProperty("Operation", "RegistrationCancelled"))
        using (LogContext.PushProperty("EntityType", "Registration"))
        using (LogContext.PushProperty("EventId", domainEvent.EventId))
        using (LogContext.PushProperty("AttendeeId", domainEvent.AttendeeId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "RegistrationCancelled START: Event={EventId}, User={UserId}",
                domainEvent.EventId, domainEvent.AttendeeId);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Retrieve user and event data
                var user = await _userRepository.GetByIdAsync(domainEvent.AttendeeId, cancellationToken);
                if (user == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "RegistrationCancelled: User not found - UserId={UserId}, Duration={ElapsedMs}ms",
                        domainEvent.AttendeeId, stopwatch.ElapsedMilliseconds);
                    return;
                }

                var @event = await _eventRepository.GetByIdAsync(domainEvent.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "RegistrationCancelled: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        domainEvent.EventId, stopwatch.ElapsedMilliseconds);
                    return;
                }

                // Phase 6A.87: Use typed email parameters for compile-time safety
                // Note: RegistrationCancelledEvent doesn't include RegistrationId, using Guid.Empty as placeholder
                var emailParams = RegistrationCancellationEmailParams.Create(
                    userId: user.Id,
                    userName: $"{user.FirstName} {user.LastName}",
                    userEmail: user.Email.Value,
                    registrationId: Guid.Empty,  // RegistrationCancelledEvent doesn't include RegistrationId
                    eventId: @event.Id,
                    eventTitle: @event.Title.Value,
                    eventStartDate: @event.StartDate,
                    timeZoneId: @event.TimeZoneId,
                    eventLocation: GetEventLocationString(@event),
                    cancellationReason: "User cancelled registration",
                    cancelledAt: domainEvent.CancelledAt,
                    refundStatus: "No Refund Required"  // Default, will be updated by refund handler if applicable
                );
                emailParams.EventDetailsUrl = _emailUrlHelper.BuildEventDetailsUrl(@event.Id);

                // Phase 6A.87: Send via typed email service (feature flags handled internally)
                var typedResult = await _typedEmailService.SendEmailAsync(
                    emailParams,
                    HandlerName,
                    cancellationToken);

                stopwatch.Stop();

                if (!typedResult.Success)
                {
                    _logger.LogError(
                        "RegistrationCancelled FAILED: Email sending failed - Email={Email}, Errors={Errors}, Duration={ElapsedMs}ms",
                        user.Email.Value, string.Join(", ", typedResult.Errors), stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogInformation(
                        "RegistrationCancelled COMPLETE: Email sent successfully - Email={Email}, UsedTyped={UsedTyped}, Duration={ElapsedMs}ms",
                        user.Email.Value, typedResult.UsedTypedParameters, stopwatch.ElapsedMilliseconds);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "RegistrationCancelled CANCELED: Operation was canceled - EventId={EventId}, UserId={UserId}, Duration={ElapsedMs}ms",
                    domainEvent.EventId, domainEvent.AttendeeId, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // Fail-silent pattern: Log error but don't throw to prevent transaction rollback
                _logger.LogError(ex,
                    "RegistrationCancelled FAILED: Exception occurred - EventId={EventId}, UserId={UserId}, Duration={ElapsedMs}ms",
                    domainEvent.EventId, domainEvent.AttendeeId, stopwatch.ElapsedMilliseconds);
            }
        }
    }

    /// <summary>
    /// Phase 6A.83: Helper method to safely extract event location string
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
