using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Contracts.LegacyPromotions; // 4C.h prereq: cycle-break
using LankaConnect.Products.LankaEvents.Application.Common; // (2026-07-09 Day 4)
namespace LankaConnect.Products.LankaEvents.Application.Common;

/// <summary>
/// Detailed registration information including attendee details
/// Used to show users their full registration with attendee names and ages
/// </summary>
public record RegistrationDetailsDto
{
    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    public Guid? UserId { get; init; }
    public int Quantity { get; init; }
    public RegistrationStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    // Session 21: Multi-attendee details
    public List<AttendeeDetailsDto> Attendees { get; init; } = new();

    // Contact information
    public string? ContactEmail { get; init; }
    public string? ContactPhone { get; init; }
    public string? ContactAddress { get; init; }

    // Payment information
    public PaymentStatus PaymentStatus { get; init; }
    public decimal? TotalPriceAmount { get; init; }
    public string? TotalPriceCurrency { get; init; }

    // Phase 6A.81 Part 3: Checkout session information for Preliminary registrations
    /// <summary>
    /// Stripe checkout session ID (stored in DB). Used to retrieve checkout URL from Stripe.
    /// Null if not Preliminary or no session created.
    /// </summary>
    public string? StripeCheckoutSessionId { get; init; }

    /// <summary>
    /// Stripe checkout URL for resuming payment (only for Preliminary status).
    /// Retrieved from Stripe at query time for security (not stored in DB).
    /// Null if registration is not Preliminary or session ID is missing.
    /// </summary>
    public string? StripeCheckoutUrl { get; init; }

    /// <summary>
    /// Timestamp when the Stripe checkout session expires (24 hours from creation).
    /// Used for countdown timer in UI. Null if not Preliminary or no session created.
    /// </summary>
    public DateTime? CheckoutSessionExpiresAt { get; init; }

    // Phase 6A.137F-Fix: Financial breakdown for bundled checkout items
    // These are populated from separate entities (donations, add-ons, collections, sponsors)
    // that share the same Stripe checkout session as the registration.
    public decimal? DonationAmount { get; init; }
    public decimal? AddOnTotal { get; init; }
    public decimal? CollectionTotal { get; init; }
    public decimal? SponsorTotal { get; init; }
    /// <summary>
    /// Grand total = TotalPriceAmount (tickets) + DonationAmount + AddOnTotal + CollectionTotal + SponsorTotal.
    /// Represents the actual amount charged by Stripe. Null if no bundled items.
    /// </summary>
    public decimal? GrandTotal { get; init; }

    // ────────────────────────────────────────────────────────────────────
    // Phase 7F-E.2 (architect-approved 2026-05-01): mode-aware fields so
    // the FE can render the "You're Registered!" card consistently
    // across Mode A and Mode B (B1/B2/B3/B4).
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Phase 7F-E.2: Snapshotted registration mode at construction time. Drives the
    /// FE rendering branch for the breakdown card.
    /// </summary>
    public RegistrationMode RegistrationMode { get; init; } = RegistrationMode.DetailedAttendees;

    /// <summary>
    /// Phase 7F-E.2: Lead attendee name (Mode B only — null for Mode A which uses the
    /// per-attendee list). Surfaced for the FE card's headline.
    /// </summary>
    public string? LeadAttendeeName { get; init; }

    /// <summary>
    /// Phase 7F-E.2: Server-projected breakdown produced by
    /// <c>RegistrationBreakdownFormatter</c>. Single shared shape consumed by event-
    /// detail card, confirmation email, and PDF ticket. <c>null</c> only when the
    /// registration has neither a HeadCount nor any Attendees (defensive).
    /// </summary>
    public RegistrationBreakdown? Breakdown { get; init; }
}

/// <summary>
/// Individual attendee details with age category and optional gender
/// Phase 6A.48: Made AgeCategory nullable to handle legacy/corrupted JSONB data
/// </summary>
public record AttendeeDetailsDto
{
    public string Name { get; init; } = string.Empty;
    public AgeCategory? AgeCategory { get; init; } // Nullable to handle data integrity issues
    public Gender? Gender { get; init; }

    // Phase 6A.161: Ticket tier the attendee was registered under. Denormalized name is
    // already persisted per-attendee in the registrations.attendees JSONB (since migration
    // 20260415203751_AddTicketTiers), so no join to ticket_tiers is needed to surface it.
    // Nullable: null for single-tier events, free events, and legacy pre-tier registrations.
    public Guid? TicketTierId { get; init; }
    public string? TicketTierName { get; init; }
}
