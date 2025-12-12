using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Phase 6A.24: Service interface for generating PDF tickets
/// </summary>
public interface IPdfTicketService
{
    /// <summary>
    /// Generates a PDF ticket with event details and QR code
    /// </summary>
    /// <param name="ticketData">Data required to generate the ticket</param>
    /// <returns>PDF document bytes</returns>
    Result<byte[]> GenerateTicketPdf(TicketPdfData ticketData);
}

/// <summary>
/// Data transfer object containing all information needed to generate a PDF ticket
/// </summary>
public record TicketPdfData
{
    /// <summary>
    /// Unique ticket code (e.g., "LC-2024-ABC123")
    /// </summary>
    public required string TicketCode { get; init; }

    /// <summary>
    /// QR code image as base64 string
    /// </summary>
    public required string QrCodeBase64 { get; init; }

    /// <summary>
    /// Event title
    /// </summary>
    public required string EventTitle { get; init; }

    /// <summary>
    /// Event start date and time
    /// </summary>
    public required DateTime EventStartDate { get; init; }

    /// <summary>
    /// Event end date and time
    /// </summary>
    public required DateTime EventEndDate { get; init; }

    /// <summary>
    /// Event location (address or "Online Event")
    /// </summary>
    public required string EventLocation { get; init; }

    /// <summary>
    /// Name of the attendee or contact
    /// </summary>
    public required string AttendeeName { get; init; }

    /// <summary>
    /// Number of attendees on this ticket
    /// </summary>
    public required int AttendeeCount { get; init; }

    /// <summary>
    /// List of attendee details (name and age)
    /// </summary>
    public required IReadOnlyList<AttendeeInfo> Attendees { get; init; }

    /// <summary>
    /// Total amount paid
    /// </summary>
    public required decimal AmountPaid { get; init; }

    /// <summary>
    /// Date when payment was completed
    /// </summary>
    public required DateTime PaymentDate { get; init; }

    /// <summary>
    /// Attendee information for the ticket
    /// </summary>
    public record AttendeeInfo(string Name, int Age);
}
