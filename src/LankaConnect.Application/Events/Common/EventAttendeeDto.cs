using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Application.Events.Common;

/// <summary>
/// DTO representing a single registration with all attendee details for export/management.
/// </summary>
public class EventAttendeeDto
{
    // Registration Info
    public Guid RegistrationId { get; init; }
    public Guid? UserId { get; init; }
    public RegistrationStatus Status { get; init; }
    public PaymentStatus PaymentStatus { get; init; }
    public DateTime CreatedAt { get; init; }

    // Contact Info
    public string ContactEmail { get; init; } = string.Empty;
    public string ContactPhone { get; init; } = string.Empty;
    public string? ContactAddress { get; init; }

    // Attendee Details
    public List<AttendeeDetailsDto> Attendees { get; init; } = new();
    public int TotalAttendees { get; init; }
    public int AdultCount { get; init; }
    public int ChildCount { get; init; }
    public string GenderDistribution { get; init; } = string.Empty;

    // Payment Info
    public decimal? TotalAmount { get; init; }
    public string? Currency { get; init; }

    // Ticket Info
    public string? TicketCode { get; init; }
    public string? QrCodeData { get; init; }
    public bool HasTicket { get; init; }

    // Computed Properties
    public string MainAttendeeName => Attendees.FirstOrDefault()?.Name ?? "Unknown";
    public string AdditionalAttendees => TotalAttendees > 1
        ? string.Join(", ", Attendees.Skip(1).Select(a => a.Name))
        : "—";
}
