using System.Diagnostics;
using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Constants;
using LankaConnect.Application.Common.Helpers;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Users;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Phase 6A.92: Handles RefundRequestedEvent to send refund notification email to user.
/// Triggered when a refund is initiated (either by user cancellation or event cancellation).
/// </summary>
public class RefundRequestedEventHandler : INotificationHandler<DomainEventNotification<RefundRequestedEvent>>
{
    private readonly IEmailService _emailService;
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<RefundRequestedEventHandler> _logger;

    private const string SupportEmail = "support@lankaconnect.com";

    public RefundRequestedEventHandler(
        IEmailService emailService,
        IUserRepository userRepository,
        IEventRepository eventRepository,
        ILogger<RefundRequestedEventHandler> logger)
    {
        _emailService = emailService;
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<RefundRequestedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        using (LogContext.PushProperty("Operation", "RefundRequested"))
        using (LogContext.PushProperty("EntityType", "Registration"))
        using (LogContext.PushProperty("EventId", domainEvent.EventId))
        using (LogContext.PushProperty("RegistrationId", domainEvent.RegistrationId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "[Phase 6A.92] RefundRequested START: EventId={EventId}, RegistrationId={RegId}, UserId={UserId}, Amount=${Amount}",
                domainEvent.EventId, domainEvent.RegistrationId, domainEvent.UserId, domainEvent.RefundAmount);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Get event details
                var @event = await _eventRepository.GetByIdAsync(domainEvent.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "[Phase 6A.92] RefundRequested: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        domainEvent.EventId, stopwatch.ElapsedMilliseconds);
                    return;
                }

                // Determine user name for email
                string userName = "Valued Customer";
                if (domainEvent.UserId.HasValue)
                {
                    var user = await _userRepository.GetByIdAsync(domainEvent.UserId.Value, cancellationToken);
                    if (user != null)
                    {
                        userName = $"{user.FirstName} {user.LastName}";
                    }
                }

                // Build email parameters
                var parameters = new Dictionary<string, object>
                {
                    { "UserName", userName },
                    { "EventTitle", @event.Title?.Value ?? "Event" },
                    { "EventDateTime", EmailDateTimeHelper.FormatDateTimeWithTz(@event.StartDate, @event.TimeZoneId) },  // Phase 6A.97: Uses event's timezone
                    { "RefundAmount", domainEvent.RefundAmount.ToString("F2") },
                    { "OrganizerContactName", @event.OrganizerContactName ?? "Event Organizer" },
                    { "OrganizerContactEmail", @event.OrganizerContactEmail ?? SupportEmail },
                    { "OrganizerContactPhone", @event.OrganizerContactPhone ?? "" },
                    { "SupportEmail", SupportEmail }
                };

                // Send templated email
                var result = await _emailService.SendTemplatedEmailAsync(
                    EmailTemplateNames.RefundRequested,
                    domainEvent.ContactEmail,
                    parameters,
                    cancellationToken);

                stopwatch.Stop();

                if (result.IsFailure)
                {
                    _logger.LogError(
                        "[Phase 6A.92] RefundRequested FAILED: Email sending failed - Email={Email}, Errors={Errors}, Duration={ElapsedMs}ms",
                        domainEvent.ContactEmail, string.Join(", ", result.Errors), stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogInformation(
                        "[Phase 6A.92] RefundRequested COMPLETE: Email sent successfully - Email={Email}, Amount=${Amount}, Duration={ElapsedMs}ms",
                        domainEvent.ContactEmail, domainEvent.RefundAmount, stopwatch.ElapsedMilliseconds);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "[Phase 6A.92] RefundRequested CANCELED: Operation was canceled - EventId={EventId}, RegistrationId={RegId}, Duration={ElapsedMs}ms",
                    domainEvent.EventId, domainEvent.RegistrationId, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // Fail-silent pattern: Log error but don't throw to prevent transaction rollback
                _logger.LogError(ex,
                    "[Phase 6A.92] RefundRequested FAILED: Exception occurred - EventId={EventId}, RegistrationId={RegId}, Duration={ElapsedMs}ms",
                    domainEvent.EventId, domainEvent.RegistrationId, stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
