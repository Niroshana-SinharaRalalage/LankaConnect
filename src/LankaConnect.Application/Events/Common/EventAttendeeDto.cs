using LankaConnect.Products.LankaEvents.Domain.Enums;

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
    // Phase 7E.7: Switched from `init` to `set` for the demographic fields so the post-processing
    // pass can override them for Mode B registrations after the SQL projection materialises.
    // (The custom JSONB ValueConverter on Registration.HeadCount prevents SQL-side sub-field access.)
    public List<AttendeeDetailsDto> Attendees { get; init; } = new();
    public int TotalAttendees { get; set; }
    public int AdultCount { get; set; }
    public int ChildCount { get; set; }
    public int MaleCount { get; set; }
    public int FemaleCount { get; set; }
    public string GenderDistribution { get; set; } = string.Empty;

    // Phase 7E.7: Mode-aware fields. Populated by GetEventAttendeesQueryHandler.
    /// <summary>Snapshot of the registration's mode (DetailedAttendees / HeadCountOnly / etc.).</summary>
    public RegistrationMode RegistrationMode { get; init; } = RegistrationMode.DetailedAttendees;

    /// <summary>For Mode B (B1-B4): the lead attendee's name. Null for Mode A.</summary>
    public string? LeadAttendeeName { get; init; }

    /// <summary>
    /// Pre-rendered demographic breakdown line for Mode B, e.g. "2 adults · 1 child".
    /// Empty string for Mode A and B1 (B1 has no demographic axis). Mirrors the email
    /// rendering for consistency. Populated in post-processing.
    /// </summary>
    public string HeadCountBreakdownLine { get; set; } = string.Empty;

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
    /// <summary>
    /// Phase 7E.7: For Mode B, returns the snapshotted LeadAttendeeName. For Mode A, falls back
    /// to the first attendee's name. "Unknown" only when neither is populated (legacy edge cases).
    /// </summary>
    public string MainAttendeeName =>
        !string.IsNullOrWhiteSpace(LeadAttendeeName)
            ? LeadAttendeeName!
            : Attendees.FirstOrDefault()?.Name ?? "Unknown";

    /// <summary>
    /// Phase 7E.7: For Mode A, comma-joined names of attendees beyond the first. For Mode B,
    /// returns "+N attendees" or empty if Total is 1 (just the lead). Em-dash for Mode A
    /// single-attendee preserves the existing UX.
    /// </summary>
    public string AdditionalAttendees
    {
        get
        {
            if (RegistrationMode != RegistrationMode.DetailedAttendees && TotalAttendees > 1)
                return $"+{TotalAttendees - 1} attendee{(TotalAttendees - 1 == 1 ? "" : "s")}";
            if (TotalAttendees > 1)
                return string.Join(", ", Attendees.Skip(1).Select(a => a.Name));
            return "—";
        }
    }

    /// <summary>
    /// Phase 6A.161: Registration-level ticket-tier summary for the Attendees table and exports.
    /// Distinct, first-appearance-ordered tier names across this registration's attendees:
    /// a single name when uniform ("VIP"), comma-joined when mixed ("VIP, General"), and the
    /// em-dash "—" when no attendee carries a tier (single-tier/free/legacy events, or Mode B
    /// head-count registrations with an empty Attendees list). Never returns null or blank.
    /// Computed (not stored) so it can never drift from the per-attendee TicketTierName detail.
    /// </summary>
    public string TicketTierSummary
    {
        get
        {
            var tierNames = Attendees
                .Where(a => !string.IsNullOrWhiteSpace(a.TicketTierName))
                .Select(a => a.TicketTierName!.Trim())
                .Distinct()
                .ToList();

            return tierNames.Count > 0 ? string.Join(", ", tierNames) : "—";
        }
    }
}
