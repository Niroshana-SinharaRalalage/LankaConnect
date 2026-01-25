using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Users;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.Commands.ResendAttendeeConfirmation;

/// <summary>
/// Phase 6A.X: Handler for resending attendee confirmation emails (Organizer action).
/// Validates organizer ownership and delegates email logic to shared service.
/// Supports both free and paid events automatically.
/// </summary>
public class ResendAttendeeConfirmationCommandHandler : ICommandHandler<ResendAttendeeConfirmationCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITicketService _ticketService;
    private readonly IRegistrationEmailService _registrationEmailService;
    private readonly ILogger<ResendAttendeeConfirmationCommandHandler> _logger;

    public ResendAttendeeConfirmationCommandHandler(
        IEventRepository eventRepository,
        IRegistrationRepository registrationRepository,
        IUserRepository userRepository,
        ITicketService ticketService,
        IRegistrationEmailService registrationEmailService,
        ILogger<ResendAttendeeConfirmationCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _registrationRepository = registrationRepository;
        _userRepository = userRepository;
        _ticketService = ticketService;
        _registrationEmailService = registrationEmailService;
        _logger = logger;
    }

    public async Task<Result> Handle(ResendAttendeeConfirmationCommand request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "ResendAttendeeConfirmation START: EventId={EventId}, RegistrationId={RegistrationId}, OrganizerId={OrganizerId}",
                request.EventId, request.RegistrationId, request.OrganizerId);

            // 1. Get registration
            var registration = await _registrationRepository.GetByIdAsync(request.RegistrationId, cancellationToken);
            if (registration == null)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "ResendAttendeeConfirmation FAILED: Registration not found - RegistrationId={RegistrationId}, Duration={ElapsedMs}ms",
                    request.RegistrationId, stopwatch.ElapsedMilliseconds);
                return Result.Failure("Registration not found");
            }

            // 2. Get event
            var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
            if (@event == null)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "ResendAttendeeConfirmation FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                    request.EventId, stopwatch.ElapsedMilliseconds);
                return Result.Failure("Event not found");
            }

            // 3. AUTHORIZATION: Validate organizer owns event
            if (@event.OrganizerId != request.OrganizerId)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "ResendAttendeeConfirmation FORBIDDEN: Organizer does not own event - EventId={EventId}, OrganizerId={OrganizerId}, ActualOrganizerId={ActualOrganizerId}, Duration={ElapsedMs}ms",
                    request.EventId, request.OrganizerId, @event.OrganizerId, stopwatch.ElapsedMilliseconds);
                return Result.Failure("Not authorized - only event organizer can resend confirmations");
            }

            // 4. Validate registration belongs to event
            if (registration.EventId != request.EventId)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "ResendAttendeeConfirmation FAILED: Registration does not belong to event - RegistrationId={RegistrationId}, EventId={EventId}, ActualEventId={ActualEventId}, Duration={ElapsedMs}ms",
                    request.RegistrationId, request.EventId, registration.EventId, stopwatch.ElapsedMilliseconds);
                return Result.Failure("Registration does not belong to this event");
            }

            // 5. Validate registration status
            if (registration.Status != RegistrationStatus.Confirmed)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "ResendAttendeeConfirmation FAILED: Registration status not Confirmed - RegistrationId={RegistrationId}, Status={Status}, Duration={ElapsedMs}ms",
                    request.RegistrationId, registration.Status, stopwatch.ElapsedMilliseconds);
                return Result.Failure($"Can only resend confirmations for Confirmed registrations. Current status: {registration.Status}");
            }

            // 6. Get user (if authenticated registration)
            Domain.Users.User? user = null;
            if (registration.UserId.HasValue)
            {
                user = await _userRepository.GetByIdAsync(registration.UserId.Value, cancellationToken);
                _logger.LogInformation(
                    "ResendAttendeeConfirmation: User retrieved - UserId={UserId}, RegistrationId={RegistrationId}",
                    registration.UserId.Value, request.RegistrationId);
            }
            else
            {
                _logger.LogInformation(
                    "ResendAttendeeConfirmation: Anonymous registration - RegistrationId={RegistrationId}",
                    request.RegistrationId);
            }

            // 7. Branch by payment status and delegate to shared service
            Result emailResult;
            if (registration.PaymentStatus == PaymentStatus.Completed)
            {
                // PAID EVENT: Get/generate ticket and send email with PDF
                _logger.LogInformation(
                    "ResendAttendeeConfirmation: Paid event - fetching ticket - RegistrationId={RegistrationId}",
                    request.RegistrationId);

                // Try to get existing ticket
                var ticket = await _ticketService.GetTicketByRegistrationIdAsync(registration.Id, cancellationToken);

                if (ticket == null)
                {
                    // Try to generate ticket (handles case where payment completed but ticket creation failed)
                    _logger.LogWarning(
                        "ResendAttendeeConfirmation: Ticket not found, attempting to generate - RegistrationId={RegistrationId}",
                        request.RegistrationId);

                    var generateResult = await _ticketService.GenerateTicketAsync(registration.Id, @event.Id, cancellationToken);
                    if (generateResult.IsFailure)
                    {
                        stopwatch.Stop();
                        _logger.LogError(
                            "ResendAttendeeConfirmation FAILED: Could not generate ticket - RegistrationId={RegistrationId}, Error={Error}, Duration={ElapsedMs}ms",
                            request.RegistrationId, generateResult.Error, stopwatch.ElapsedMilliseconds);
                        return Result.Failure($"Could not generate ticket: {generateResult.Error}");
                    }

                    // Get the generated ticket entity
                    ticket = await _ticketService.GetTicketByIdAsync(generateResult.Value.TicketId, cancellationToken);

                    if (ticket == null)
                    {
                        stopwatch.Stop();
                        _logger.LogError(
                            "ResendAttendeeConfirmation FAILED: Ticket not found after generation - TicketId={TicketId}, Duration={ElapsedMs}ms",
                            generateResult.Value.TicketId, stopwatch.ElapsedMilliseconds);
                        return Result.Failure("Ticket not found after generation");
                    }

                    _logger.LogInformation(
                        "ResendAttendeeConfirmation: Ticket generated successfully - TicketCode={TicketCode}",
                        ticket.TicketCode);
                }
                else
                {
                    _logger.LogInformation(
                        "ResendAttendeeConfirmation: Existing ticket found - TicketCode={TicketCode}",
                        ticket.TicketCode);
                }

                // Get ticket PDF
                var pdfResult = await _ticketService.GetTicketPdfAsync(ticket.Id, cancellationToken);
                if (pdfResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogError(
                        "ResendAttendeeConfirmation FAILED: Could not get ticket PDF - TicketId={TicketId}, Error={Error}, Duration={ElapsedMs}ms",
                        ticket.Id, pdfResult.Error, stopwatch.ElapsedMilliseconds);
                    return Result.Failure($"Could not get ticket PDF: {pdfResult.Error}");
                }

                _logger.LogInformation(
                    "ResendAttendeeConfirmation: Sending paid event confirmation - TicketCode={TicketCode}, PdfSize={PdfSize}",
                    ticket.TicketCode, pdfResult.Value.Length);

                emailResult = await _registrationEmailService.SendPaidEventConfirmationEmailAsync(
                    registration, @event, ticket, pdfResult.Value, user, cancellationToken);
            }
            else if (registration.PaymentStatus == PaymentStatus.NotRequired)
            {
                // FREE EVENT: Send simple confirmation
                _logger.LogInformation(
                    "ResendAttendeeConfirmation: Free event - sending confirmation - RegistrationId={RegistrationId}",
                    request.RegistrationId);

                emailResult = await _registrationEmailService.SendFreeEventConfirmationEmailAsync(
                    registration, @event, user, cancellationToken);
            }
            else
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "ResendAttendeeConfirmation FAILED: Invalid payment status - RegistrationId={RegistrationId}, PaymentStatus={PaymentStatus}, Duration={ElapsedMs}ms",
                    request.RegistrationId, registration.PaymentStatus, stopwatch.ElapsedMilliseconds);
                return Result.Failure($"Cannot resend confirmation - payment status is {registration.PaymentStatus}");
            }

            stopwatch.Stop();

            if (emailResult.IsSuccess)
            {
                _logger.LogInformation(
                    "ResendAttendeeConfirmation COMPLETE: Email sent successfully - RegistrationId={RegistrationId}, PaymentStatus={PaymentStatus}, Duration={ElapsedMs}ms",
                    request.RegistrationId, registration.PaymentStatus, stopwatch.ElapsedMilliseconds);
                return Result.Success();
            }
            else
            {
                _logger.LogError(
                    "ResendAttendeeConfirmation FAILED: Email sending failed - RegistrationId={RegistrationId}, Error={Error}, Duration={ElapsedMs}ms",
                    request.RegistrationId, emailResult.Error, stopwatch.ElapsedMilliseconds);
                return emailResult;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "ResendAttendeeConfirmation CANCELLED: Operation was cancelled - EventId={EventId}, RegistrationId={RegistrationId}, Duration={ElapsedMs}ms",
                request.EventId, request.RegistrationId, stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "ResendAttendeeConfirmation FAILED: Exception occurred - EventId={EventId}, RegistrationId={RegistrationId}, Duration={ElapsedMs}ms",
                request.EventId, request.RegistrationId, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
