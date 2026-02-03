using System.Diagnostics;
using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Constants;
using LankaConnect.Application.Common.Helpers;
using LankaConnect.Application.Common.Interfaces;
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
/// Phase 6A.92: Handles RefundRequestedEvent to send refund notification email to user.
/// Triggered when a refund is initiated (either by user cancellation or event cancellation).
/// Phase 6A.87: Migrated to ITypedEmailService for hybrid email support
/// </summary>
public class RefundRequestedEventHandler : INotificationHandler<DomainEventNotification<RefundRequestedEvent>>
{
    private readonly IEmailService _emailService;
    private readonly ITypedEmailService _typedEmailService;  // Phase 6A.87: Typed email service
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<RefundRequestedEventHandler> _logger;

    private const string SupportEmail = "support@lankaconnect.com";
    private const string HandlerName = nameof(RefundRequestedEventHandler);  // Phase 6A.87: For feature flag lookup

    public RefundRequestedEventHandler(
        IEmailService emailService,
        ITypedEmailService typedEmailService,  // Phase 6A.87: Typed email service
        IUserRepository userRepository,
        IEventRepository eventRepository,
        ILogger<RefundRequestedEventHandler> logger)
    {
        _emailService = emailService;
        _typedEmailService = typedEmailService;  // Phase 6A.87: Typed email service
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
                Guid userId = Guid.Empty;
                if (domainEvent.UserId.HasValue)
                {
                    var user = await _userRepository.GetByIdAsync(domainEvent.UserId.Value, cancellationToken);
                    if (user != null)
                    {
                        userName = $"{user.FirstName} {user.LastName}";
                        userId = user.Id;
                    }
                }

                // Phase 6A.87: Use typed email parameters for compile-time safety
                var emailParams = RefundEmailParams.CreateRequest(
                    userId: userId,
                    userName: userName,
                    userEmail: domainEvent.ContactEmail,
                    registrationId: domainEvent.RegistrationId,
                    refundId: Guid.NewGuid(),  // Refund ID not available in domain event yet
                    eventId: @event.Id,
                    eventTitle: @event.Title?.Value ?? "Event",
                    eventStartDate: @event.StartDate,
                    timeZoneId: @event.TimeZoneId,
                    refundAmount: domainEvent.RefundAmount,
                    originalAmount: domainEvent.RefundAmount,  // Same as refund for full refunds
                    refundReason: "Registration Cancellation",
                    requestedAt: DateTime.UtcNow
                );
                emailParams.SupportEmail = SupportEmail;

                // Phase 6A.87: Send via typed email service (feature flags handled internally)
                var typedResult = await _typedEmailService.SendEmailAsync(
                    emailParams,
                    HandlerName,
                    cancellationToken);

                stopwatch.Stop();

                if (!typedResult.Success)
                {
                    _logger.LogError(
                        "[Phase 6A.87] RefundRequested FAILED: Email sending failed - Email={Email}, Errors={Errors}, Duration={ElapsedMs}ms",
                        domainEvent.ContactEmail, string.Join(", ", typedResult.Errors), stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogInformation(
                        "[Phase 6A.87] RefundRequested COMPLETE: Email sent successfully - Email={Email}, Amount=${Amount}, UsedTyped={UsedTyped}, Duration={ElapsedMs}ms",
                        domainEvent.ContactEmail, domainEvent.RefundAmount, typedResult.UsedTypedParameters, stopwatch.ElapsedMilliseconds);
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
