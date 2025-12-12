using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.Commands.ResendTicketEmail;

/// <summary>
/// Phase 6A.24: Command to resend ticket email to registration contact
/// </summary>
public record ResendTicketEmailCommand(Guid EventId, Guid RegistrationId, Guid UserId) : IRequest<Result<Unit>>;

/// <summary>
/// Phase 6A.24: Handler for resending ticket email
/// </summary>
public class ResendTicketEmailCommandHandler : IRequestHandler<ResendTicketEmailCommand, Result<Unit>>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IEmailService _emailService;
    private readonly ITicketService _ticketService;
    private readonly ILogger<ResendTicketEmailCommandHandler> _logger;

    public ResendTicketEmailCommandHandler(
        ITicketRepository ticketRepository,
        IRegistrationRepository registrationRepository,
        IEventRepository eventRepository,
        IEmailService emailService,
        ITicketService ticketService,
        ILogger<ResendTicketEmailCommandHandler> logger)
    {
        _ticketRepository = ticketRepository;
        _registrationRepository = registrationRepository;
        _eventRepository = eventRepository;
        _emailService = emailService;
        _ticketService = ticketService;
        _logger = logger;
    }

    public async Task<Result<Unit>> Handle(ResendTicketEmailCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Resending ticket email for registration {RegistrationId}, event {EventId}, user {UserId}",
            request.RegistrationId, request.EventId, request.UserId);

        // Get registration and verify ownership
        var registration = await _registrationRepository.GetByIdAsync(request.RegistrationId, cancellationToken);
        if (registration == null)
        {
            _logger.LogWarning("Registration {RegistrationId} not found", request.RegistrationId);
            return Result<Unit>.Failure("Registration not found");
        }

        // Verify the user owns this registration
        if (registration.UserId != request.UserId)
        {
            _logger.LogWarning("User {UserId} does not own registration {RegistrationId}",
                request.UserId, request.RegistrationId);
            return Result<Unit>.Failure("You are not authorized to resend this ticket");
        }

        // Get ticket for the registration
        var ticket = await _ticketRepository.GetByRegistrationIdAsync(request.RegistrationId, cancellationToken);
        if (ticket == null)
        {
            _logger.LogWarning("Ticket not found for registration {RegistrationId}", request.RegistrationId);
            return Result<Unit>.Failure("Ticket not found for this registration");
        }

        // Get event details
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
        {
            _logger.LogWarning("Event {EventId} not found", request.EventId);
            return Result<Unit>.Failure("Event not found");
        }

        // Get contact email
        var contactEmail = registration.Contact?.Email ?? registration.AttendeeInfo?.Email?.Value;
        if (string.IsNullOrEmpty(contactEmail))
        {
            _logger.LogWarning("No contact email found for registration {RegistrationId}", request.RegistrationId);
            return Result<Unit>.Failure("No contact email found for this registration");
        }

        // Get contact name
        var contactName = registration.HasDetailedAttendees() && registration.Attendees.Any()
            ? registration.Attendees.First().Name
            : "Guest";

        // Prepare attendee details
        var attendeeDetails = new List<Dictionary<string, object>>();
        if (registration.HasDetailedAttendees())
        {
            foreach (var attendee in registration.Attendees)
            {
                attendeeDetails.Add(new Dictionary<string, object>
                {
                    { "Name", attendee.Name },
                    { "Age", attendee.Age }
                });
            }
        }

        // Generate ticket PDF
        var pdfResult = await _ticketService.GetTicketPdfAsync(ticket.Id, cancellationToken);
        if (pdfResult.IsFailure)
        {
            _logger.LogError("Failed to generate ticket PDF for ticket {TicketId}: {Error}", ticket.Id, pdfResult.Error);
            return Result<Unit>.Failure($"Failed to generate ticket PDF: {pdfResult.Error}");
        }
        var pdfBase64 = Convert.ToBase64String(pdfResult.Value);

        // Prepare email parameters
        var parameters = new Dictionary<string, object>
        {
            { "UserName", contactName },
            { "EventTitle", @event.Title.Value },
            { "EventStartDate", @event.StartDate.ToString("MMMM dd, yyyy") },
            { "EventStartTime", @event.StartDate.ToString("h:mm tt") },
            { "EventEndDate", @event.EndDate.ToString("MMMM dd, yyyy") },
            { "EventLocation", @event.Location != null ? $"{@event.Location.Address.Street}, {@event.Location.Address.City}" : "Online Event" },
            { "RegistrationDate", registration.CreatedAt.ToString("MMMM dd, yyyy h:mm tt") },
            { "Attendees", attendeeDetails },
            { "HasAttendeeDetails", attendeeDetails.Any() },
            { "TicketCode", ticket.TicketCode },
            { "TotalPaid", registration.TotalPrice?.Amount ?? 0 },
            { "Currency", registration.TotalPrice?.Currency.ToString() ?? "USD" },
            { "TicketPdfBase64", pdfBase64 }
        };

        // Send templated email
        var result = await _emailService.SendTemplatedEmailAsync(
            "TicketConfirmation",
            contactEmail,
            parameters,
            cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogError("Failed to resend ticket email to {Email}: {Errors}",
                contactEmail, string.Join(", ", result.Errors));
            return Result<Unit>.Failure($"Failed to send email: {string.Join(", ", result.Errors)}");
        }

        _logger.LogInformation("Successfully resent ticket email to {Email} for ticket {TicketId}",
            contactEmail, ticket.Id);

        return Result<Unit>.Success(Unit.Value);
    }
}
