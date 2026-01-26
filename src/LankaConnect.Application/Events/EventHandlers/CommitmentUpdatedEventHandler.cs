using System.Diagnostics;
using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Constants;
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
/// Phase 6A.51+: Handles CommitmentUpdatedEvent to send update confirmation email to user
/// when they change their commitment quantity or details.
/// </summary>
public class CommitmentUpdatedEventHandler : INotificationHandler<DomainEventNotification<CommitmentUpdatedEvent>>
{
    private readonly IEmailService _emailService;
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IEmailUrlHelper _emailUrlHelper;
    private readonly ILogger<CommitmentUpdatedEventHandler> _logger;

    public CommitmentUpdatedEventHandler(
        IEmailService emailService,
        IUserRepository userRepository,
        IEventRepository eventRepository,
        IEmailUrlHelper emailUrlHelper,
        ILogger<CommitmentUpdatedEventHandler> logger)
    {
        _emailService = emailService;
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _emailUrlHelper = emailUrlHelper;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<CommitmentUpdatedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        using (LogContext.PushProperty("Operation", "CommitmentUpdated"))
        using (LogContext.PushProperty("EntityType", "SignUpCommitment"))
        using (LogContext.PushProperty("UserId", domainEvent.UserId))
        using (LogContext.PushProperty("SignUpItemId", domainEvent.SignUpItemId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "CommitmentUpdated START: UserId={UserId}, ItemDescription={ItemDescription}, OldQuantity={OldQuantity}, NewQuantity={NewQuantity}",
                domainEvent.UserId, domainEvent.ItemDescription, domainEvent.OldQuantity, domainEvent.NewQuantity);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Get user details
                var user = await _userRepository.GetByIdAsync(domainEvent.UserId, cancellationToken);
                if (user == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "CommitmentUpdated: User not found - UserId={UserId}, Duration={ElapsedMs}ms",
                        domainEvent.UserId, stopwatch.ElapsedMilliseconds);
                    return; // Fail-silent: don't throw to prevent transaction rollback
                }

                // Get event details via repository navigation method
                var @event = await _eventRepository.GetEventBySignUpItemIdAsync(domainEvent.SignUpItemId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "CommitmentUpdated: Event not found - SignUpItemId={SignUpItemId}, Duration={ElapsedMs}ms",
                        domainEvent.SignUpItemId, stopwatch.ElapsedMilliseconds);
                    return; // Fail-silent
                }

            // Build template parameters
            // Phase 6A.83 Part 3: Fix parameter names to match template expectations
            var templateData = new Dictionary<string, object>
            {
                { "UserName", user.FirstName },
                { "EventTitle", @event.Title?.Value ?? "Untitled Event" },  // Phase 6A.83: Extract Value from value object
                { "ItemName", domainEvent.ItemDescription },  // Phase 6A.83: Changed from ItemDescription to ItemName
                { "OldQuantity", domainEvent.OldQuantity },
                { "Quantity", domainEvent.NewQuantity },  // Phase 6A.83: Changed from NewQuantity to Quantity
                { "EventDateTime", @event.StartDate.ToString("f") },
                { "EventLocation", @event.Location?.ToString() ?? "Location TBD" },
                { "ManageCommitmentUrl", $"{_emailUrlHelper.BuildEventDetailsUrl(@event.Id)}#sign-ups" },  // Phase 6A.83: Added missing parameter
                { "PickupInstructions", "Please coordinate pickup/delivery details with the event organizer." }
            };

                // Send templated email
                var result = await _emailService.SendTemplatedEmailAsync(
                    EmailTemplateNames.SignupCommitmentUpdate,
                    user.Email.Value,
                    templateData,
                    cancellationToken);

                stopwatch.Stop();

                if (result.IsSuccess)
                {
                    _logger.LogInformation(
                        "CommitmentUpdated COMPLETE: Email sent successfully - Email={Email}, EventId={EventId}, Duration={ElapsedMs}ms",
                        user.Email.Value, @event.Id, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogError(
                        "CommitmentUpdated FAILED: Email sending failed - Email={Email}, Error={Error}, Duration={ElapsedMs}ms",
                        user.Email.Value, result.Error, stopwatch.ElapsedMilliseconds);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "CommitmentUpdated CANCELED: Operation was canceled - UserId={UserId}, SignUpItemId={SignUpItemId}, Duration={ElapsedMs}ms",
                    domainEvent.UserId, domainEvent.SignUpItemId, stopwatch.ElapsedMilliseconds);
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // Fail-silent: Log error but don't throw to prevent transaction rollback
                _logger.LogError(ex,
                    "CommitmentUpdated FAILED: Exception occurred - UserId={UserId}, SignUpItemId={SignUpItemId}, Duration={ElapsedMs}ms",
                    domainEvent.UserId, domainEvent.SignUpItemId, stopwatch.ElapsedMilliseconds);
            }
        }
    }
}
