using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Application.Events.Common;

/// <summary>
/// Phase 6A.24: Data transfer object for event tickets
/// </summary>
public record TicketDto
{
    public Guid Id { get; init; }
    public Guid RegistrationId { get; init; }
    public Guid EventId { get; init; }
    public Guid? UserId { get; init; }
    public string TicketCode { get; init; } = string.Empty;
    public string? QrCodeBase64 { get; init; }
    public string? PdfBlobUrl { get; init; }
    public bool IsValid { get; init; }
    public DateTime? ValidatedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public DateTime CreatedAt { get; init; }

    // Event details for display
    public string? EventTitle { get; init; }
    public DateTime? EventStartDate { get; init; }
    public string? EventLocation { get; init; }

    // Attendee information
    public int AttendeeCount { get; init; }
    public List<TicketAttendeeDto>? Attendees { get; init; }
}

/// <summary>
/// Phase 6A.24: Attendee information for ticket display with age category and gender
/// Phase 6A.48: Made AgeCategory nullable to handle legacy/corrupted JSONB data
/// </summary>
public record TicketAttendeeDto
{
    public string Name { get; init; } = string.Empty;
    public AgeCategory? AgeCategory { get; init; } // Nullable to handle data integrity issues
    public Gender? Gender { get; init; }
}
