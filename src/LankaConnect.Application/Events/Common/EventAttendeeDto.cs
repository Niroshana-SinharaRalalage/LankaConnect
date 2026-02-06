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
    /// <summary>
    /// Phase 6A.71: GROSS amount (what customer paid, before commission).
    /// For organizer payout, use NetAmount instead.
    /// </summary>
    public decimal? TotalAmount { get; init; }

    /// <summary>
    /// Phase 6A.71: NET amount (organizer's payout after 5% platform commission).
    /// Null for free events or registrations without payment.
    /// Phase 6A.X FIX: Changed to { get; set; } to allow on-the-fly calculation update in query handler.
    /// </summary>
    public decimal? NetAmount { get; set; }

    public string? Currency { get; init; }

    // Phase 6A.X: Per-registration revenue breakdown
    /// <summary>
    /// Sales tax amount for this registration (state + local).
    /// Null for registrations without breakdown data or free events.
    /// Phase 6A.X FIX: Changed to { get; set; } to allow on-the-fly calculation in query handler.
    /// </summary>
    public decimal? SalesTaxAmount { get; set; }

    /// <summary>
    /// Stripe processing fee for this registration (2.9% + $0.30).
    /// Null for registrations without breakdown data or free events.
    /// Phase 6A.X FIX: Changed to { get; set; } to allow on-the-fly calculation in query handler.
    /// </summary>
    public decimal? StripeFeeAmount { get; set; }

    /// <summary>
    /// Platform commission for this registration (2% of taxable amount).
    /// Null for registrations without breakdown data or free events.
    /// Phase 6A.X FIX: Changed to { get; set; } to allow on-the-fly calculation in query handler.
    /// </summary>
    public decimal? PlatformCommissionAmount { get; set; }

    /// <summary>
    /// Organizer payout for this registration (after tax, Stripe fee, platform commission).
    /// Should equal NetAmount for registrations with breakdown data.
    /// Null for registrations without breakdown data or free events.
    /// Phase 6A.X FIX: Changed to { get; set; } to allow on-the-fly calculation in query handler.
    /// </summary>
    public decimal? OrganizerPayoutAmount { get; set; }

    /// <summary>
    /// Sales tax rate applied to this registration (e.g., 0.0725 for 7.25%).
    /// Zero for registrations without tax or free events.
    /// Phase 6A.X FIX: Changed to { get; set; } to allow on-the-fly calculation in query handler.
    /// </summary>
    public decimal SalesTaxRate { get; set; }

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
