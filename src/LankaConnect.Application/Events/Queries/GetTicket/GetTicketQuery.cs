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
/// </summary>
public class GetTicketQueryHandler : IRequestHandler<GetTicketQuery, Result<TicketDto>>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IQrCodeService _qrCodeService;
    private readonly ILogger<GetTicketQueryHandler> _logger;

    public GetTicketQueryHandler(
        ITicketRepository ticketRepository,
        IRegistrationRepository registrationRepository,
        IEventRepository eventRepository,
        IQrCodeService qrCodeService,
        ILogger<GetTicketQueryHandler> logger)
    {
        _ticketRepository = ticketRepository;
        _registrationRepository = registrationRepository;
        _eventRepository = eventRepository;
        _qrCodeService = qrCodeService;
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

        // Get ticket for the registration
        var ticket = await _ticketRepository.GetByRegistrationIdAsync(request.RegistrationId, cancellationToken);
        if (ticket == null)
        {
            _logger.LogWarning("Ticket not found for registration {RegistrationId}", request.RegistrationId);
            return Result<TicketDto>.Failure("Ticket not found for this registration");
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
