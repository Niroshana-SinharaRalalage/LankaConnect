using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Users;

namespace LankaConnect.API.Controllers;

/// <summary>
/// TEMPORARY CONTROLLER: Administrative recovery operations
///
/// PURPOSE: Recover from failed webhook processing due to IPublisher NULL bug in old revision
///
/// SCENARIO:
/// - Webhook evt_1SfSktLvfbr023L1qB78D1CR processed by old Container App revision
/// - Payment completed successfully, but domain event NOT dispatched (IPublisher was NULL)
/// - No email sent, no ticket generated
/// - Idempotency flag prevents reprocessing
///
/// SECURITY:
/// - Requires Authorization
/// - Should be removed after successful recovery
/// - Logs all operations for audit trail
///
/// DELETE THIS FILE AFTER RECOVERY IS COMPLETE
/// </summary>
[ApiController]
[Route("api/admin/recovery")]
[Authorize] // TODO: Add admin role requirement in production
public class AdminRecoveryController : ControllerBase
{
    private readonly IPublisher _publisher;
    private readonly IEventRepository _eventRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<AdminRecoveryController> _logger;

    public AdminRecoveryController(
        IPublisher publisher,
        IEventRepository eventRepository,
        IRegistrationRepository registrationRepository,
        IUserRepository userRepository,
        ILogger<AdminRecoveryController> logger)
    {
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        _eventRepository = eventRepository;
        _registrationRepository = registrationRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    /// <summary>
    /// Manually triggers PaymentCompletedEvent for a specific registration
    ///
    /// USE CASE: Recover from webhook processing failures where payment succeeded
    /// but email/ticket generation failed due to infrastructure issues.
    ///
    /// SAFETY:
    /// - Verifies registration exists and payment is completed
    /// - Idempotent: Can be called multiple times safely
    /// - Does NOT modify payment status or registration status
    /// - Only triggers email/ticket generation
    /// </summary>
    /// <param name="request">Recovery request with registration ID</param>
    /// <returns>200 OK if event published successfully</returns>
    [HttpPost("trigger-payment-event")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TriggerPaymentCompletedEvent([FromBody] TriggerPaymentEventRequest request)
    {
        _logger.LogWarning(
            "[ADMIN RECOVERY] Manual PaymentCompletedEvent trigger requested for Registration {RegistrationId}",
            request.RegistrationId);

        try
        {
            // Step 1: Retrieve registration
            var registration = await _registrationRepository.GetByIdAsync(request.RegistrationId);
            if (registration == null)
            {
                _logger.LogWarning("Registration {RegistrationId} not found", request.RegistrationId);
                return NotFound(new { Error = "Registration not found" });
            }

            // Step 2: Verify payment is completed (safety check)
            if (registration.PaymentStatus != Domain.Events.Enums.PaymentStatus.Completed)
            {
                _logger.LogWarning(
                    "Registration {RegistrationId} has PaymentStatus {Status}, expected Completed",
                    request.RegistrationId,
                    registration.PaymentStatus);

                return BadRequest(new
                {
                    Error = "Payment not completed",
                    Details = $"Registration has payment status: {registration.PaymentStatus}. Only Completed payments can trigger this event."
                });
            }

            // Step 3: Verify registration is confirmed
            if (registration.Status != Domain.Events.Enums.RegistrationStatus.Confirmed)
            {
                _logger.LogWarning(
                    "Registration {RegistrationId} has Status {Status}, expected Confirmed",
                    request.RegistrationId,
                    registration.Status);

                return BadRequest(new
                {
                    Error = "Registration not confirmed",
                    Details = $"Registration has status: {registration.Status}. Only Confirmed registrations can trigger this event."
                });
            }

            // Step 4: Get event details
            var @event = await _eventRepository.GetByIdAsync(registration.EventId);
            if (@event == null)
            {
                _logger.LogError("Event {EventId} not found for Registration {RegistrationId}",
                    registration.EventId, request.RegistrationId);
                return NotFound(new { Error = "Event not found" });
            }

            // Step 5: Determine contact email
            string contactEmail;
            if (registration.UserId.HasValue)
            {
                var user = await _userRepository.GetByIdAsync(registration.UserId.Value);
                contactEmail = user?.Email.Value ?? registration.Contact?.Email ?? string.Empty;
            }
            else
            {
                contactEmail = registration.Contact?.Email ?? string.Empty;
            }

            if (string.IsNullOrEmpty(contactEmail))
            {
                _logger.LogError("No contact email found for Registration {RegistrationId}", request.RegistrationId);
                return BadRequest(new { Error = "Contact email not found" });
            }

            // Step 6: Verify required data
            if (string.IsNullOrEmpty(registration.StripePaymentIntentId))
            {
                _logger.LogWarning("Registration {RegistrationId} missing StripePaymentIntentId", request.RegistrationId);
                return BadRequest(new { Error = "Payment Intent ID not found" });
            }

            if (registration.TotalPrice == null)
            {
                _logger.LogWarning("Registration {RegistrationId} missing TotalPrice", request.RegistrationId);
                return BadRequest(new { Error = "Total price not found" });
            }

            // Step 7: Construct PaymentCompletedEvent
            var paymentCompletedEvent = new PaymentCompletedEvent(
                EventId: registration.EventId,
                RegistrationId: registration.Id,
                UserId: registration.UserId,
                ContactEmail: contactEmail,
                PaymentIntentId: registration.StripePaymentIntentId,
                AmountPaid: registration.TotalPrice.Amount,
                AttendeeCount: registration.GetAttendeeCount(),
                PaymentCompletedAt: registration.UpdatedAt ?? registration.CreatedAt
            );

            _logger.LogWarning(
                "[ADMIN RECOVERY] Publishing PaymentCompletedEvent - Event: {EventId}, Registration: {RegistrationId}, Email: {Email}, Amount: {Amount}",
                paymentCompletedEvent.EventId,
                paymentCompletedEvent.RegistrationId,
                paymentCompletedEvent.ContactEmail,
                paymentCompletedEvent.AmountPaid);

            // Step 8: Publish domain event via MediatR
            var notification = new DomainEventNotification<PaymentCompletedEvent>(paymentCompletedEvent);
            await _publisher.Publish(notification);

            _logger.LogWarning(
                "[ADMIN RECOVERY] Successfully published PaymentCompletedEvent for Registration {RegistrationId}",
                request.RegistrationId);

            return Ok(new
            {
                Message = "PaymentCompletedEvent published successfully",
                RegistrationId = registration.Id,
                EventId = registration.EventId,
                ContactEmail = contactEmail,
                AmountPaid = registration.TotalPrice.Amount,
                AttendeeCount = registration.GetAttendeeCount(),
                Note = "Email and ticket generation should complete within 30 seconds. Check application logs for PaymentCompletedEventHandler."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[ADMIN RECOVERY] Error triggering PaymentCompletedEvent for Registration {RegistrationId}",
                request.RegistrationId);

            return StatusCode(500, new
            {
                Error = "Internal server error",
                Details = ex.Message
            });
        }
    }
}

/// <summary>
/// Request to manually trigger PaymentCompletedEvent
/// </summary>
public class TriggerPaymentEventRequest
{
    /// <summary>
    /// Registration ID for which to trigger the event
    /// </summary>
    public required Guid RegistrationId { get; init; }
}
