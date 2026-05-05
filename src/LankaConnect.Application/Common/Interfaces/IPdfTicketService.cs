using LankaConnect.Application.Events.Common;
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
    /// IANA timezone identifier for converting UTC dates to event's local time display.
    /// Null defaults to "America/New_York" (Eastern Time).
    /// </summary>
    public string? TimeZoneId { get; init; }

    /// <summary>
    /// Ticket type label for display on PDF (e.g., "General Admission", "VIP", "2x VIP, 3x Basic")
    /// </summary>
    public string? TicketType { get; init; }

    /// <summary>
    /// Phase 7F-E.4a: Cross-surface registration breakdown — populated for both Mode A
    /// (DetailedAttendees) and Mode B (HeadCount*) registrations via
    /// <see cref="LankaConnect.Application.Events.Common.TicketPdfRegistrationBreakdownAssembler"/>.
    ///
    /// When non-null, the PDF renderer surfaces a "Registration Breakdown" section with
    /// per-tier rows showing Adult/Child + Male/Female counts (or "N/A" when the mode
    /// doesn't capture that axis). Architect "in addition to" rule: Mode A keeps the
    /// existing per-attendee list AND ALSO renders the breakdown summary; Mode B uses
    /// the breakdown as the primary attendee section (the per-attendee list is empty).
    /// </summary>
    public RegistrationBreakdown? RegistrationBreakdown { get; init; }

    /// <summary>
    /// Attendee information for the ticket
    /// Phase 6A.43: Updated to use AgeCategory string instead of numeric Age
    /// Phase 8: Added TierName for multi-tier ticketing
    /// Slice 7 S7.7: Added SeatLabel for assigned-seating events. Null when
    /// the event is general-admission or the attendee has no seat yet.
    /// </summary>
    public record AttendeeInfo(
        string Name,
        string AgeCategory,
        string? TierName = null,
        string? SeatLabel = null);
}
