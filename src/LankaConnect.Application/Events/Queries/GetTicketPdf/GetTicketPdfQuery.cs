using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.Queries.GetTicketPdf;

/// <summary>
/// Phase 6A.24: Query to get ticket PDF for download
/// </summary>
public record GetTicketPdfQuery(Guid EventId, Guid RegistrationId, Guid UserId) : IRequest<Result<TicketPdfResult>>;

/// <summary>
/// Phase 6A.24: Result containing the PDF bytes and filename
/// </summary>
public record TicketPdfResult(byte[] PdfBytes, string FileName);

/// <summary>
/// Phase 6A.24: Handler for getting ticket PDF
/// </summary>
public class GetTicketPdfQueryHandler : IRequestHandler<GetTicketPdfQuery, Result<TicketPdfResult>>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ITicketService _ticketService;
    private readonly ILogger<GetTicketPdfQueryHandler> _logger;

    public GetTicketPdfQueryHandler(
        ITicketRepository ticketRepository,
        IRegistrationRepository registrationRepository,
        IEventRepository eventRepository,
        ITicketService ticketService,
        ILogger<GetTicketPdfQueryHandler> logger)
    {
        _ticketRepository = ticketRepository;
        _registrationRepository = registrationRepository;
        _eventRepository = eventRepository;
        _ticketService = ticketService;
        _logger = logger;
    }

    public async Task<Result<TicketPdfResult>> Handle(GetTicketPdfQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting ticket PDF for registration {RegistrationId}, event {EventId}, user {UserId}",
            request.RegistrationId, request.EventId, request.UserId);

        // Get registration and verify ownership
        var registration = await _registrationRepository.GetByIdAsync(request.RegistrationId, cancellationToken);
        if (registration == null)
        {
            _logger.LogWarning("Registration {RegistrationId} not found", request.RegistrationId);
            return Result<TicketPdfResult>.Failure("Registration not found");
        }

        // Verify the user owns this registration
        if (registration.UserId != request.UserId)
        {
            _logger.LogWarning("User {UserId} does not own registration {RegistrationId}",
                request.UserId, request.RegistrationId);
            return Result<TicketPdfResult>.Failure("You are not authorized to download this ticket");
        }

        // Get ticket for the registration
        var ticket = await _ticketRepository.GetByRegistrationIdAsync(request.RegistrationId, cancellationToken);
        if (ticket == null)
        {
            _logger.LogWarning("Ticket not found for registration {RegistrationId}", request.RegistrationId);
            return Result<TicketPdfResult>.Failure("Ticket not found for this registration");
        }

        // Get event details for the PDF
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
        {
            _logger.LogWarning("Event {EventId} not found", request.EventId);
            return Result<TicketPdfResult>.Failure("Event not found");
        }

        // Get PDF bytes (will regenerate if needed)
        var pdfResult = await _ticketService.GetTicketPdfAsync(ticket.Id, cancellationToken);
        if (pdfResult.IsFailure)
        {
            _logger.LogError("Failed to get ticket PDF for ticket {TicketId}: {Error}", ticket.Id, pdfResult.Error);
            return Result<TicketPdfResult>.Failure($"Failed to generate ticket PDF: {pdfResult.Error}");
        }

        var pdfBytes = pdfResult.Value;

        var fileName = $"ticket-{ticket.TicketCode}.pdf";

        _logger.LogInformation("Successfully generated ticket PDF for ticket {TicketId}, size: {Size} bytes",
            ticket.Id, pdfBytes.Length);

        return Result<TicketPdfResult>.Success(new TicketPdfResult(pdfBytes, fileName));
    }
}
