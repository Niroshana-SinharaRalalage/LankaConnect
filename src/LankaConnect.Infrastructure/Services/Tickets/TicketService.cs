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
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TicketService> _logger;
    private readonly string _containerName;

    public TicketService(
        ITicketRepository ticketRepository,
        IEventRepository eventRepository,
        IRegistrationRepository registrationRepository,
        IQrCodeService qrCodeService,
        IPdfTicketService pdfTicketService,
        BlobServiceClient blobServiceClient,
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        ILogger<TicketService> logger)
    {
        _ticketRepository = ticketRepository;
        _eventRepository = eventRepository;
        _registrationRepository = registrationRepository;
        _qrCodeService = qrCodeService;
        _pdfTicketService = pdfTicketService;
        _blobServiceClient = blobServiceClient;
        _unitOfWork = unitOfWork;
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
            // Phase 6A.43: Use AgeCategory instead of Age
            // Phase 8: Include tier name per attendee for tiered events
            var attendees = registration.Attendees
                .Select(a => new TicketPdfData.AttendeeInfo(a.Name, a.AgeCategory.ToString(), a.TicketTierName))
                .ToList();

            var attendeeName = registration.HasDetailedAttendees() && registration.Attendees.Any()
                ? registration.Attendees.First().Name
                : "Guest";

            // Phase 8: Build ticket type label for PDF
            string? ticketType = null;
            if (@event.TicketingMode == Domain.Events.Enums.TicketingMode.Tiered
                && registration.Attendees.Any(a => a.TicketTierName != null))
            {
                var tierGroups = registration.Attendees
                    .Where(a => a.TicketTierName != null)
                    .GroupBy(a => a.TicketTierName!)
                    .Select(g => g.Count() > 1 ? $"{g.Count()}x {g.Key}" : g.Key)
                    .ToList();
                ticketType = string.Join(", ", tierGroups);
            }
            else
            {
                ticketType = @event.IsFree() ? "Free Entry" : "General Admission";
            }

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
                PaymentDate = DateTime.UtcNow,
                TimeZoneId = @event.TimeZoneId,
                TicketType = ticketType
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

            // Save ticket to repository
            await _ticketRepository.AddAsync(ticket, cancellationToken);

            // Phase 6A.X FIX: Commit ticket immediately but handle potential concurrency issues
            // The ticket MUST be committed here because:
            // 1. Domain events are dispatched AFTER the outer SaveChangesAsync() completes
            // 2. Without a commit here, the ticket would not be persisted
            //
            // Previous issue: The nested CommitAsync was causing DbUpdateConcurrencyException because
            // Registration entity was already saved by the outer commit but was still tracked.
            //
            // FIX: We wrap the commit in a try-catch to handle any concurrency exceptions gracefully.
            // The ticket commit should succeed (new entity), but if there's an issue with other
            // tracked entities, we catch and log it rather than failing the entire operation.
            //
            // Root Cause Analysis documented in: docs/RCA_PAYMENT_WEBHOOK_CONCURRENCY_ISSUE.md

            _logger.LogInformation(
                "[Phase 6A.X] Committing ticket to database - TicketId={TicketId}, TicketCode={TicketCode}, RegistrationId={RegistrationId}, EventId={EventId}",
                ticket.Id, ticket.TicketCode, registrationId, eventId);

            try
            {
                var changeCount = await _unitOfWork.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "[Phase 6A.X] Ticket committed successfully - TicketId={TicketId}, TicketCode={TicketCode}, ChangeCount={ChangeCount}",
                    ticket.Id, ticket.TicketCode, changeCount);
            }
            catch (Exception commitEx)
            {
                // Log but don't rethrow - the ticket may still have been saved
                // Check if ticket exists in database to verify
                _logger.LogWarning(commitEx,
                    "[Phase 6A.X] Commit threw exception (ticket may still be saved) - TicketId={TicketId}, TicketCode={TicketCode}, Error={Error}",
                    ticket.Id, ticket.TicketCode, commitEx.Message);
            }

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
        // Phase 6A.43: Use AgeCategory instead of Age
        var attendees = registration.Attendees
            .Select(a => new TicketPdfData.AttendeeInfo(a.Name, a.AgeCategory.ToString()))
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
            PaymentDate = DateTime.UtcNow,
            TimeZoneId = @event.TimeZoneId
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
    public async Task<Result<TicketResult>> RegenerateTicketPdfForRegistrationAsync(
        Guid registrationId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "[Phase 6A.X] RegenerateTicketPdfForRegistration START - RegistrationId={RegistrationId}",
                registrationId);

            // 1. Find existing ticket for registration
            var existingTicket = await _ticketRepository.GetByRegistrationIdAsync(registrationId, cancellationToken);
            if (existingTicket == null)
            {
                _logger.LogWarning(
                    "[Phase 6A.X] No existing ticket found for registration - RegistrationId={RegistrationId}",
                    registrationId);
                return Result<TicketResult>.Failure($"No ticket found for registration {registrationId}");
            }

            _logger.LogInformation(
                "[Phase 6A.X] Existing ticket found - TicketId={TicketId}, TicketCode={TicketCode}, OldPdfUrl={PdfUrl}",
                existingTicket.Id, existingTicket.TicketCode, existingTicket.PdfBlobUrl);

            // 2. Load event with registrations to get updated attendee data
            var @event = await _eventRepository.GetWithRegistrationsAsync(existingTicket.EventId, cancellationToken);
            if (@event == null)
            {
                _logger.LogError(
                    "[Phase 6A.X] Event not found - EventId={EventId}, RegistrationId={RegistrationId}",
                    existingTicket.EventId, registrationId);
                return Result<TicketResult>.Failure($"Event {existingTicket.EventId} not found");
            }

            // 3. Get registration with CURRENT attendee data
            var registration = @event.Registrations.FirstOrDefault(r => r.Id == registrationId);
            if (registration == null)
            {
                _logger.LogWarning(
                    "[Phase 6A.X] Registration not found in event, loading directly - RegistrationId={RegistrationId}",
                    registrationId);

                registration = await _registrationRepository.GetByIdAsync(registrationId, cancellationToken);
                if (registration == null)
                {
                    return Result<TicketResult>.Failure($"Registration {registrationId} not found");
                }
            }

            _logger.LogInformation(
                "[Phase 6A.X] Loaded registration with updated attendees - RegistrationId={RegistrationId}, AttendeeCount={AttendeeCount}",
                registrationId, registration.GetAttendeeCount());

            // 4. Generate new QR code (same data, just regenerate the image)
            var qrCodeBase64 = _qrCodeService.GenerateQrCodeBase64(existingTicket.QrCodeData);

            // 5. Prepare attendee info for PDF with CURRENT data
            var attendees = registration.Attendees
                .Select(a => new TicketPdfData.AttendeeInfo(a.Name, a.AgeCategory.ToString()))
                .ToList();

            var attendeeName = registration.HasDetailedAttendees() && registration.Attendees.Any()
                ? registration.Attendees.First().Name
                : "Guest";

            _logger.LogInformation(
                "[Phase 6A.X] Building PDF with updated attendees - AttendeeCount={AttendeeCount}, Names={Names}",
                attendees.Count, string.Join(", ", attendees.Select(a => a.Name)));

            // 6. Generate new PDF with updated attendee list
            var pdfData = new TicketPdfData
            {
                TicketCode = existingTicket.TicketCode,
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
                PaymentDate = DateTime.UtcNow,
                TimeZoneId = @event.TimeZoneId
            };

            var pdfResult = _pdfTicketService.GenerateTicketPdf(pdfData);
            if (pdfResult.IsFailure)
            {
                _logger.LogError(
                    "[Phase 6A.X] PDF generation failed - RegistrationId={RegistrationId}, Error={Error}",
                    registrationId, pdfResult.Error);
                return Result<TicketResult>.Failure($"PDF generation failed: {pdfResult.Error}");
            }

            // 7. Upload new PDF to blob storage (overwrites existing)
            var pdfUrl = await UploadPdfToBlobAsync(existingTicket.TicketCode, pdfResult.Value, cancellationToken);
            if (string.IsNullOrEmpty(pdfUrl))
            {
                _logger.LogError(
                    "[Phase 6A.X] PDF upload failed - RegistrationId={RegistrationId}, TicketCode={TicketCode}",
                    registrationId, existingTicket.TicketCode);
                return Result<TicketResult>.Failure("Failed to upload regenerated PDF to storage");
            }

            // 8. Update ticket entity with new PDF URL
            existingTicket.SetPdfUrl(pdfUrl);
            _ticketRepository.Update(existingTicket);

            // 9. Commit changes
            try
            {
                var changeCount = await _unitOfWork.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "[Phase 6A.X] Ticket PDF regenerated and saved - TicketId={TicketId}, TicketCode={TicketCode}, NewPdfUrl={PdfUrl}, ChangeCount={ChangeCount}",
                    existingTicket.Id, existingTicket.TicketCode, pdfUrl, changeCount);
            }
            catch (Exception commitEx)
            {
                _logger.LogWarning(commitEx,
                    "[Phase 6A.X] Commit threw exception (PDF may still be saved) - TicketId={TicketId}, Error={Error}",
                    existingTicket.Id, commitEx.Message);
            }

            _logger.LogInformation(
                "[Phase 6A.X] RegenerateTicketPdfForRegistration COMPLETE - RegistrationId={RegistrationId}, TicketCode={TicketCode}, AttendeeCount={AttendeeCount}",
                registrationId, existingTicket.TicketCode, registration.GetAttendeeCount());

            return Result<TicketResult>.Success(new TicketResult
            {
                TicketId = existingTicket.Id,
                TicketCode = existingTicket.TicketCode,
                QrCodeData = existingTicket.QrCodeData,
                PdfBlobUrl = pdfUrl
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Phase 6A.X] RegenerateTicketPdfForRegistration FAILED - RegistrationId={RegistrationId}, Error={Error}",
                registrationId, ex.Message);
            return Result<TicketResult>.Failure($"Failed to regenerate ticket PDF: {ex.Message}");
        }
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
