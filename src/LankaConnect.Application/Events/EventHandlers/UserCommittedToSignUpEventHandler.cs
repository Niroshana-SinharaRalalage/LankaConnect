using System.Diagnostics;
using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Constants;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Users;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Phase 6A.51: Handles UserCommittedToSignUpEvent to send confirmation email to user
/// when they commit to bringing an item to an event.
/// </summary>
public class UserCommittedToSignUpEventHandler : INotificationHandler<DomainEventNotification<UserCommittedToSignUpEvent>>
{
    private readonly IEmailService _emailService;
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<UserCommittedToSignUpEventHandler> _logger;

    public UserCommittedToSignUpEventHandler(
        IEmailService emailService,
        IUserRepository userRepository,
        IEventRepository eventRepository,
        ILogger<UserCommittedToSignUpEventHandler> logger)
    {
        _emailService = emailService;
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<UserCommittedToSignUpEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        using (LogContext.PushProperty("Operation", "UserCommittedToSignUp"))
        using (LogContext.PushProperty("EntityType", "SignUpCommitment"))
        using (LogContext.PushProperty("UserId", domainEvent.UserId))
        using (LogContext.PushProperty("SignUpListId", domainEvent.SignUpListId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "UserCommittedToSignUp START: UserId={UserId}, Quantity={Quantity}, ItemDescription={ItemDescription}, SignUpListId={SignUpListId}",
                domainEvent.UserId, domainEvent.Quantity, domainEvent.ItemDescription, domainEvent.SignUpListId);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Get user details
                var user = await _userRepository.GetByIdAsync(domainEvent.UserId, cancellationToken);
                if (user == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UserCommittedToSignUp: User not found - UserId={UserId}, Duration={ElapsedMs}ms",
                        domainEvent.UserId, stopwatch.ElapsedMilliseconds);
                    return; // Fail-silent: don't throw to prevent transaction rollback
                }

                // Get event details via repository navigation method
                var @event = await _eventRepository.GetEventBySignUpListIdAsync(domainEvent.SignUpListId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UserCommittedToSignUp: Event not found - SignUpListId={SignUpListId}, Duration={ElapsedMs}ms",
                        domainEvent.SignUpListId, stopwatch.ElapsedMilliseconds);
                    return; // Fail-silent
                }

            // Build template parameters
            var templateData = new Dictionary<string, object>
            {
                { "UserName", user.FirstName },
                { "EventTitle", @event.Title },
                { "ItemDescription", domainEvent.ItemDescription },
                { "Quantity", domainEvent.Quantity },
                { "EventDateTime", @event.StartDate.ToString("f") }, // Full date/time pattern - fixed placeholder name
                { "EventLocation", @event.Location?.ToString() ?? "Location TBD" },
                { "PickupInstructions", "Please coordinate pickup/delivery details with the event organizer." } // Default instruction
            };

                // Send templated email
                var result = await _emailService.SendTemplatedEmailAsync(
                    EmailTemplateNames.SignupCommitmentConfirmation,
                    user.Email.Value,
                    templateData,
                    cancellationToken);

                stopwatch.Stop();

                if (result.IsSuccess)
                {
                    _logger.LogInformation(
                        "UserCommittedToSignUp COMPLETE: Email sent successfully - Email={Email}, EventId={EventId}, Duration={ElapsedMs}ms",
                        user.Email.Value, @event.Id, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogError(
                        "UserCommittedToSignUp FAILED: Email sending failed - Email={Email}, Error={Error}, Duration={ElapsedMs}ms",
                        user.Email.Value, result.Error, stopwatch.ElapsedMilliseconds);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "UserCommittedToSignUp CANCELED: Operation was canceled - UserId={UserId}, SignUpListId={SignUpListId}, Duration={ElapsedMs}ms",
                    domainEvent.UserId, domainEvent.SignUpListId, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // Fail-silent: Log error but don't throw to prevent transaction rollback
                _logger.LogError(ex,
                    "UserCommittedToSignUp FAILED: Exception occurred - UserId={UserId}, SignUpListId={SignUpListId}, Duration={ElapsedMs}ms",
                    domainEvent.UserId, domainEvent.SignUpListId, stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
