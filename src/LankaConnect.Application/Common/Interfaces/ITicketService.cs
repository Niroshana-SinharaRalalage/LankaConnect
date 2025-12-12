using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Entities;

namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Phase 6A.24: Orchestration service for ticket operations
/// </summary>
public interface ITicketService
{
    /// <summary>
    /// Generates a complete ticket with QR code and PDF for a registration
    /// </summary>
    /// <param name="registrationId">Registration ID</param>
    /// <param name="eventId">Event ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated ticket with PDF URL</returns>
    Task<Result<TicketResult>> GenerateTicketAsync(
        Guid registrationId,
        Guid eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a ticket by its ID
    /// </summary>
    Task<Ticket?> GetTicketByIdAsync(Guid ticketId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a ticket by registration ID
    /// </summary>
    Task<Ticket?> GetTicketByRegistrationIdAsync(Guid registrationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a ticket by its ticket code
    /// </summary>
    Task<Ticket?> GetTicketByCodeAsync(string ticketCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a ticket for check-in
    /// </summary>
    Task<Result> ValidateTicketAsync(string ticketCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Regenerates the PDF for an existing ticket
    /// </summary>
    Task<Result<string>> RegeneratePdfAsync(Guid ticketId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the PDF bytes for a ticket
    /// </summary>
    Task<Result<byte[]>> GetTicketPdfAsync(Guid ticketId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of ticket generation
/// </summary>
public record TicketResult
{
    public required Guid TicketId { get; init; }
    public required string TicketCode { get; init; }
    public required string QrCodeData { get; init; }
    public string? PdfBlobUrl { get; init; }
}
