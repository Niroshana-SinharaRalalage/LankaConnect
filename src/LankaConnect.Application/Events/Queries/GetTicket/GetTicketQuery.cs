using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.Queries.GetTicket;

/// <summary>
/// Phase 6A.24: Query to get ticket details for a registration
/// </summary>
public record GetTicketQuery(Guid EventId, Guid RegistrationId, Guid UserId) : IRequest<Result<TicketDto>>;

/// <summary>
/// Phase 6A.24: Handler for getting ticket details
/// Phase 6A.24 FIX: Now also generates ticket if payment is complete but ticket was never created
/// </summary>
public class GetTicketQueryHandler : IRequestHandler<GetTicketQuery, Result<TicketDto>>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IQrCodeService _qrCodeService;
    private readonly ITicketService _ticketService;
    private readonly ILogger<GetTicketQueryHandler> _logger;

    public GetTicketQueryHandler(
        ITicketRepository ticketRepository,
        IRegistrationRepository registrationRepository,
        IEventRepository eventRepository,
        IQrCodeService qrCodeService,
        ITicketService ticketService,
        ILogger<GetTicketQueryHandler> logger)
    {
        _ticketRepository = ticketRepository;
        _registrationRepository = registrationRepository;
        _eventRepository = eventRepository;
        _qrCodeService = qrCodeService;
        _ticketService = ticketService;
        _logger = logger;
    }

    public async Task<Result<TicketDto>> Handle(GetTicketQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting ticket for registration {RegistrationId}, event {EventId}, user {UserId}",
            request.RegistrationId, request.EventId, request.UserId);

        // Get registration and verify ownership
        var registration = await _registrationRepository.GetByIdAsync(request.RegistrationId, cancellationToken);
        if (registration == null)
        {
            _logger.LogWarning("Registration {RegistrationId} not found", request.RegistrationId);
            return Result<TicketDto>.Failure("Registration not found");
        }

        // Verify the user owns this registration
        if (registration.UserId != request.UserId)
        {
            _logger.LogWarning("User {UserId} does not own registration {RegistrationId}",
                request.UserId, request.RegistrationId);
            return Result<TicketDto>.Failure("You are not authorized to view this ticket");
        }

        // Phase 6A.24 FIX: Get or generate ticket
        // If ticket doesn't exist (webhook initially failed), generate it now
        var ticket = await _ticketRepository.GetByRegistrationIdAsync(request.RegistrationId, cancellationToken);
        if (ticket == null)
        {
            _logger.LogInformation("No ticket found for registration {RegistrationId}, attempting to generate...",
                request.RegistrationId);

            // Check if payment is complete before generating
            if (registration.PaymentStatus != Domain.Events.Enums.PaymentStatus.Completed)
            {
                _logger.LogWarning("Cannot generate ticket - payment not completed for registration {RegistrationId}",
                    request.RegistrationId);
                return Result<TicketDto>.Failure("Payment not completed - ticket cannot be generated");
            }

            // Generate ticket now
            var generateResult = await _ticketService.GenerateTicketAsync(
                request.RegistrationId,
                request.EventId,
                cancellationToken);

            if (generateResult.IsFailure)
            {
                _logger.LogError("Failed to generate ticket for registration {RegistrationId}: {Error}",
                    request.RegistrationId, string.Join(", ", generateResult.Errors));
                return Result<TicketDto>.Failure("Failed to generate ticket");
            }

            // Retrieve the newly generated ticket
            ticket = await _ticketRepository.GetByIdAsync(generateResult.Value.TicketId, cancellationToken);
            _logger.LogInformation("Ticket generated successfully: {TicketCode}", generateResult.Value.TicketCode);

            if (ticket == null)
            {
                _logger.LogError("Ticket still not found after generation for registration {RegistrationId}",
                    request.RegistrationId);
                return Result<TicketDto>.Failure("Ticket generation failed");
            }
        }

        // Get event details
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);

        // Generate QR code as base64 for display
        var qrCodeBytes = _qrCodeService.GenerateQrCode(ticket.QrCodeData);
        var qrCodeBase64 = Convert.ToBase64String(qrCodeBytes);

        // Map attendees
        var attendees = registration.HasDetailedAttendees()
            ? registration.Attendees.Select(a => new TicketAttendeeDto
            {
                Name = a.Name,
                Age = a.Age
            }).ToList()
            : null;

        var ticketDto = new TicketDto
        {
            Id = ticket.Id,
            RegistrationId = ticket.RegistrationId,
            EventId = ticket.EventId,
            UserId = ticket.UserId,
            TicketCode = ticket.TicketCode,
            QrCodeBase64 = qrCodeBase64,
            PdfBlobUrl = ticket.PdfBlobUrl,
            IsValid = ticket.IsValid,
            ValidatedAt = ticket.ValidatedAt,
            ExpiresAt = ticket.ExpiresAt,
            CreatedAt = ticket.CreatedAt,
            EventTitle = @event?.Title.Value,
            EventStartDate = @event?.StartDate,
            EventLocation = @event?.Location != null
                ? $"{@event.Location.Address.Street}, {@event.Location.Address.City}"
                : "Online Event",
            AttendeeCount = registration.GetAttendeeCount(),
            Attendees = attendees
        };

        _logger.LogInformation("Successfully retrieved ticket {TicketId} for registration {RegistrationId}",
            ticket.Id, request.RegistrationId);

        return Result<TicketDto>.Success(ticketDto);
    }
}
