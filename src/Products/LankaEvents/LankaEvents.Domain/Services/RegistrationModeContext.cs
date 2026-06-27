using LankaConnect.Products.LankaEvents.Domain.Enums;

namespace LankaConnect.Products.LankaEvents.Domain.Services;

/// <summary>
/// Phase 7E.2: Captures the event-shape axes that influence which <see cref="Enums.RegistrationMode"/>s are allowed.
///
/// Used as the input to <see cref="RegistrationModeCompatibility.Check"/> and
/// <see cref="RegistrationModeCompatibility.AllowedModes"/>. Constructed from:
/// - <c>CreateEventCommandHandler</c>: derived from the request DTO before <c>Event.Create</c>.
/// - <c>UpdateEventCommandHandler</c>: derived from the post-update event shape.
/// - <c>GetAllowedRegistrationModesQueryHandler</c>: passed as a draft from the frontend.
///
/// Forward-extensibility: fields representing axes that are not yet modelled on
/// <see cref="Event"/> (named seating discriminator, identity-bound add-on discriminator,
/// per-ticket name flag, tier × age matrix pricing) default to <c>false</c> in callers
/// today. As those fields are introduced in subsequent slices, the callers populate
/// them and the compatibility table picks up the new behaviour automatically.
/// </summary>
public sealed record RegistrationModeContext
{
    /// <summary>True when attendance is free (no per-attendee fee).</summary>
    public bool IsFreeAttendance { get; init; }

    /// <summary>
    /// Phase 8X.11 — payment locus for the event. Defaults to Free so existing callers
    /// (which don't yet pass this axis) keep their pre-Phase-8X behaviour. The value
    /// gates whether <see cref="RegistrationMode.External"/> is allowed (only ExternalPaid)
    /// and forbids the other modes for ExternalPaid.
    /// </summary>
    public EventPaymentMode PaymentMode { get; init; } = EventPaymentMode.Free;

    /// <summary>True when the event uses any seating (assigned or auto-block).</summary>
    public bool HasSeating { get; init; }

    /// <summary>
    /// True when seats must carry a named attendee (organiser explicitly opts in to per-seat names).
    /// Distinct from <see cref="HasSeating"/>: a seated event with auto-allocated blocks where
    /// each seat is unnamed sets <c>HasSeating=true</c> but <c>HasNamedSeating=false</c>.
    /// Currently always <c>false</c> in callers (no field on <c>Event</c> yet — Phase 7F).
    /// </summary>
    public bool HasNamedSeating { get; init; }

    /// <summary>
    /// True when the organiser requires each issued ticket to print a unique attendee name.
    /// Currently always <c>false</c> in callers (no field on <c>Event</c> yet — Phase 7F).
    /// </summary>
    public bool RequiresAttendeeNameOnTicket { get; init; }

    /// <summary>
    /// True when the event uses dual pricing (separate Adult and Child prices).
    /// Derived from <c>Event.Pricing.HasChildPricing</c>.
    /// </summary>
    public bool HasDualPricing { get; init; }

    /// <summary>
    /// True when the event has group-tier discount pricing (e.g. 5+ attendees = lower price).
    /// Count-based; works in any B mode.
    /// </summary>
    public bool HasGroupTiers { get; init; }

    /// <summary>
    /// True when the event has multiple ticket tiers (VIP / General / Backstage).
    /// Per-attendee tier mixing within one registration is captured via the optional
    /// <c>TierCounts</c> axis on <c>HeadCountBreakdown</c> (added in 7E.3c).
    /// </summary>
    public bool HasTicketTiers { get; init; }

    /// <summary>
    /// True when an add-on is identity-bound (e.g. engraved name plate per attendee).
    /// Currently always <c>false</c> in callers (no add-on shape discriminator on <c>Event</c> yet — Phase 7F).
    /// </summary>
    public bool HasIdentityBoundAddOn { get; init; }

    /// <summary>
    /// True when pricing varies by both tier AND age (e.g. adult-VIP $50 vs child-VIP $25).
    /// Cannot be captured by the current <c>HeadCountBreakdown</c> axes — Phase 7F.
    /// Currently always <c>false</c> in callers.
    /// </summary>
    public bool HasMatrixPricing { get; init; }
}
