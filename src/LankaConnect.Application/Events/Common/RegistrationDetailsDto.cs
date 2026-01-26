using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Application.Events.Common;

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
}
