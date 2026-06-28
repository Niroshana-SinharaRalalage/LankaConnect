namespace LankaConnect.Domain.Events.Enums;

/// <summary>
/// Phase 7E: Per-event registration capture mode chosen by the organiser.
/// Default is <see cref="DetailedAttendees"/> — the pre-7E behaviour — so existing events
/// remain unchanged.
///
/// Compatibility rules (architect-locked, 14-row table in the Phase 7E plan):
/// - Mode C (NoRegistration) is allowed iff event attendance is free AND no seating.
/// - Mode A (DetailedAttendees) is required when any of: per-ticket attendee name,
///   named seating, identity-bound add-on, tier × age matrix pricing.
/// - Modes B1–B4 capture <see cref="ValueObjects.HeadCountBreakdown"/> (Total + Demographics? + TierCounts?).
///
/// Stored as <c>smallint</c> with DB-level <c>DEFAULT 0</c> so legacy rows materialise as
/// <see cref="DetailedAttendees"/> automatically (Phase 6A.123 lesson — never rely on app-side defaults
/// for NOT NULL columns).
/// </summary>
public enum RegistrationMode : short
{
    /// <summary>Per-attendee Name + AgeCategory + Gender. Pre-7E behaviour. Default for back-compat.</summary>
    DetailedAttendees = 0,

    /// <summary>Lead attendee name + total head count. No demographic detail.</summary>
    HeadCountOnly = 1,

    /// <summary>Lead name + Adults + Children (Total derived from leaves).</summary>
    HeadCountByAge = 2,

    /// <summary>Lead name + Males + Females (Total derived from leaves).</summary>
    HeadCountByGender = 3,

    /// <summary>Lead name + AdultMales + AdultFemales + ChildMales + ChildFemales (Total derived from 4 leaves).</summary>
    HeadCountByAgeAndGender = 4,

    /// <summary>No registration UI. Drop-in event. Standalone donations/sponsors/add-ons/collections still work.</summary>
    NoRegistration = 5,

    /// <summary>
    /// Phase 8X.11 — Registration is captured externally (e.g. Eventbrite, Humanitix,
    /// organiser's own page, cash-at-door). LankaConnect renders the link / instructions
    /// in the registration section; no internal Registration row is ever created.
    /// Required when <see cref="EventPaymentMode.ExternalPaid"/>; rejected for
    /// <see cref="EventPaymentMode.Free"/> and <see cref="EventPaymentMode.OnPlatformPaid"/>.
    /// Ticket prices can still be defined and shown for reference; AssignedSeating /
    /// add-ons / waitlist / donations / sponsors / signup-lists are blocked because they
    /// require an internal Registration row.
    /// </summary>
    External = 6,
}
