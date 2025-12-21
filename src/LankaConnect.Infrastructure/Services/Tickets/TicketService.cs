using Azure.Storage.Blobs;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Infrastructure.Services.Tickets;

/// <summary>
/// Phase 6A.24: Orchestration service for ticket generation workflow
/// </summary>
public class TicketService : ITicketService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IQrCodeService _qrCodeService;
    private readonly IPdfTicketService _pdfTicketService;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<TicketService> _logger;
    private readonly string _containerName;

    public TicketService(
        ITicketRepository ticketRepository,
        IEventRepository eventRepository,
        IRegistrationRepository registrationRepository,
        IQrCodeService qrCodeService,
        IPdfTicketService pdfTicketService,
        BlobServiceClient blobServiceClient,
        IConfiguration configuration,
        ILogger<TicketService> logger)
    {
        _ticketRepository = ticketRepository;
        _eventRepository = eventRepository;
        _registrationRepository = registrationRepository;
        _qrCodeService = qrCodeService;
        _pdfTicketService = pdfTicketService;
        _blobServiceClient = blobServiceClient;
        _logger = logger;
        _containerName = configuration["AzureStorage:TicketsContainer"] ?? "tickets";
    }

    /// <inheritdoc />
    public async Task<Result<TicketResult>> GenerateTicketAsync(
        Guid registrationId,
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating ticket for Registration {RegistrationId}, Event {EventId}",
                registrationId, eventId);

            // Check if ticket already exists
            var existingTicket = await _ticketRepository.GetByRegistrationIdAsync(registrationId, cancellationToken);
            if (existingTicket != null)
            {
                _logger.LogInformation("Ticket already exists for Registration {RegistrationId}", registrationId);
                return Result<TicketResult>.Success(new TicketResult
                {
                    TicketId = existingTicket.Id,
                    TicketCode = existingTicket.TicketCode,
                    QrCodeData = existingTicket.QrCodeData,
                    PdfBlobUrl = existingTicket.PdfBlobUrl
                });
            }

            // Phase 6A.24 FIX: Use GetWithRegistrationsAsync to include registrations
            var @event = await _eventRepository.GetWithRegistrationsAsync(eventId, cancellationToken);
            if (@event == null)
            {
                return Result<TicketResult>.Failure($"Event {eventId} not found");
            }

            // Get registration details from event, with fallback to direct repository load
            var registration = @event.Registrations.FirstOrDefault(r => r.Id == registrationId);
            if (registration == null)
            {
                _logger.LogWarning("Registration {RegistrationId} not found in event {EventId} registrations. Loading directly...",
                    registrationId, eventId);

                // Fallback: Load registration directly from repository
                registration = await _registrationRepository.GetByIdAsync(registrationId, cancellationToken);
                if (registration == null)
                {
                    return Result<TicketResult>.Failure($"Registration {registrationId} not found");
                }
            }

            // Create ticket entity
            var ticketResult = Ticket.Create(
                registrationId,
                eventId,
                registration.UserId,
                @event.EndDate);

            if (ticketResult.IsFailure)
            {
                return Result<TicketResult>.Failure(ticketResult.Error);
            }

            var ticket = ticketResult.Value;

            // Ensure unique ticket code
            while (await _ticketRepository.TicketCodeExistsAsync(ticket.TicketCode, cancellationToken))
            {
                // Regenerate if collision (very rare)
                ticketResult = Ticket.Create(registrationId, eventId, registration.UserId, @event.EndDate);
                if (ticketResult.IsFailure)
                {
                    return Result<TicketResult>.Failure(ticketResult.Error);
                }
                ticket = ticketResult.Value;
            }

            // Generate QR code
            var qrCodeBase64 = _qrCodeService.GenerateQrCodeBase64(ticket.QrCodeData);

            // Prepare attendee info for PDF
            var attendees = registration.Attendees
                .Select(a => new TicketPdfData.AttendeeInfo(a.Name, a.Age))
                .ToList();

            var attendeeName = registration.HasDetailedAttendees() && registration.Attendees.Any()
                ? registration.Attendees.First().Name
                : "Guest";

            // Generate PDF
            var pdfData = new TicketPdfData
            {
                TicketCode = ticket.TicketCode,
                QrCodeBase64 = qrCodeBase64,
                EventTitle = @event.Title.Value,
                EventStartDate = @event.StartDate,
                EventEndDate = @event.EndDate,
                EventLocation = @event.Location != null
                    ? $"{@event.Location.Address.Street}, {@event.Location.Address.City}"
                    : "Online Event",
                AttendeeName = attendeeName,
                AttendeeCount = registration.GetAttendeeCount(),
                Attendees = attendees,
                AmountPaid = registration.TotalPrice?.Amount ?? 0m,
                PaymentDate = DateTime.UtcNow
            };

            var pdfResult = _pdfTicketService.GenerateTicketPdf(pdfData);
            if (pdfResult.IsFailure)
            {
                _logger.LogWarning("Failed to generate PDF for ticket {TicketCode}: {Error}",
                    ticket.TicketCode, pdfResult.Error);
                // Continue without PDF - can be regenerated later
            }
            else
            {
                // Upload PDF to Azure Blob Storage
                var pdfUrl = await UploadPdfToBlobAsync(
                    ticket.TicketCode,
                    pdfResult.Value,
                    cancellationToken);

                if (!string.IsNullOrEmpty(pdfUrl))
                {
                    ticket.SetPdfUrl(pdfUrl);
                }
            }

            // Save ticket
            await _ticketRepository.AddAsync(ticket, cancellationToken);

            _logger.LogInformation("Successfully generated ticket {TicketCode} for Registration {RegistrationId}",
                ticket.TicketCode, registrationId);

            return Result<TicketResult>.Success(new TicketResult
            {
                TicketId = ticket.Id,
                TicketCode = ticket.TicketCode,
                QrCodeData = ticket.QrCodeData,
                PdfBlobUrl = ticket.PdfBlobUrl
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate ticket for Registration {RegistrationId}, Event {EventId}",
                registrationId, eventId);
            return Result<TicketResult>.Failure($"Failed to generate ticket: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Ticket?> GetTicketByIdAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        return await _ticketRepository.GetByIdAsync(ticketId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Ticket?> GetTicketByRegistrationIdAsync(Guid registrationId, CancellationToken cancellationToken = default)
    {
        return await _ticketRepository.GetByRegistrationIdAsync(registrationId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Ticket?> GetTicketByCodeAsync(string ticketCode, CancellationToken cancellationToken = default)
    {
        return await _ticketRepository.GetByTicketCodeAsync(ticketCode, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result> ValidateTicketAsync(string ticketCode, CancellationToken cancellationToken = default)
    {
        var ticket = await _ticketRepository.GetByTicketCodeAsync(ticketCode, cancellationToken);
        if (ticket == null)
        {
            return Result.Failure("Ticket not found");
        }

        var result = ticket.Validate();
        if (result.IsSuccess)
        {
            _ticketRepository.Update(ticket);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<Result<string>> RegeneratePdfAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken);
        if (ticket == null)
        {
            return Result<string>.Failure("Ticket not found");
        }

        // Phase 6A.24 FIX: Use GetWithRegistrationsAsync to include registrations
        var @event = await _eventRepository.GetWithRegistrationsAsync(ticket.EventId, cancellationToken);
        if (@event == null)
        {
            return Result<string>.Failure("Event not found");
        }

        var registration = @event.Registrations.FirstOrDefault(r => r.Id == ticket.RegistrationId);
        if (registration == null)
        {
            _logger.LogWarning("Registration {RegistrationId} not found in event {EventId} registrations. Loading directly...",
                ticket.RegistrationId, ticket.EventId);

            // Fallback: Load registration directly from repository
            registration = await _registrationRepository.GetByIdAsync(ticket.RegistrationId, cancellationToken);
            if (registration == null)
            {
                return Result<string>.Failure("Registration not found");
            }
        }

        // Generate QR code
        var qrCodeBase64 = _qrCodeService.GenerateQrCodeBase64(ticket.QrCodeData);

        // Prepare attendee info
        var attendees = registration.Attendees
            .Select(a => new TicketPdfData.AttendeeInfo(a.Name, a.Age))
            .ToList();

        var attendeeName = registration.HasDetailedAttendees() && registration.Attendees.Any()
            ? registration.Attendees.First().Name
            : "Guest";

        var pdfData = new TicketPdfData
        {
            TicketCode = ticket.TicketCode,
            QrCodeBase64 = qrCodeBase64,
            EventTitle = @event.Title.Value,
            EventStartDate = @event.StartDate,
            EventEndDate = @event.EndDate,
            EventLocation = @event.Location != null
                ? $"{@event.Location.Address.Street}, {@event.Location.Address.City}"
                : "Online Event",
            AttendeeName = attendeeName,
            AttendeeCount = registration.GetAttendeeCount(),
            Attendees = attendees,
            AmountPaid = registration.TotalPrice?.Amount ?? 0m,
            PaymentDate = DateTime.UtcNow
        };

        var pdfResult = _pdfTicketService.GenerateTicketPdf(pdfData);
        if (pdfResult.IsFailure)
        {
            return Result<string>.Failure(pdfResult.Error);
        }

        var pdfUrl = await UploadPdfToBlobAsync(ticket.TicketCode, pdfResult.Value, cancellationToken);
        if (string.IsNullOrEmpty(pdfUrl))
        {
            return Result<string>.Failure("Failed to upload PDF to storage");
        }

        ticket.SetPdfUrl(pdfUrl);
        _ticketRepository.Update(ticket);

        return Result<string>.Success(pdfUrl);
    }

    /// <inheritdoc />
    public async Task<Result<byte[]>> GetTicketPdfAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken);
        if (ticket == null)
        {
            return Result<byte[]>.Failure("Ticket not found");
        }

        if (string.IsNullOrEmpty(ticket.PdfBlobUrl))
        {
            // Try to regenerate
            var regenerateResult = await RegeneratePdfAsync(ticketId, cancellationToken);
            if (regenerateResult.IsFailure)
            {
                return Result<byte[]>.Failure("PDF not available and regeneration failed");
            }

            ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken);
            if (ticket == null || string.IsNullOrEmpty(ticket.PdfBlobUrl))
            {
                return Result<byte[]>.Failure("Failed to get ticket PDF");
            }
        }

        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobName = GetBlobNameFromUrl(ticket.PdfBlobUrl);
            var blobClient = containerClient.GetBlobClient(blobName);

            var response = await blobClient.DownloadContentAsync(cancellationToken);
            return Result<byte[]>.Success(response.Value.Content.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download PDF for ticket {TicketId}", ticketId);
            return Result<byte[]>.Failure($"Failed to download PDF: {ex.Message}");
        }
    }

    private async Task<string?> UploadPdfToBlobAsync(
        string ticketCode,
        byte[] pdfBytes,
        CancellationToken cancellationToken)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            var blobName = $"{ticketCode}.pdf";
            var blobClient = containerClient.GetBlobClient(blobName);

            using var stream = new MemoryStream(pdfBytes);
            await blobClient.UploadAsync(stream, overwrite: true, cancellationToken);

            _logger.LogInformation("Uploaded PDF for ticket {TicketCode} to blob storage", ticketCode);
            return blobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload PDF for ticket {TicketCode} to blob storage", ticketCode);
            return null;
        }
    }

    private static string GetBlobNameFromUrl(string blobUrl)
    {
        var uri = new Uri(blobUrl);
        var segments = uri.Segments;
        // Last segment is the blob name
        return segments.Length > 0 ? segments[^1] : string.Empty;
    }
}
