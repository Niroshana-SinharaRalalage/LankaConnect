namespace LankaConnect.Products.LankaEvents.Domain.Enums;

/// <summary>
/// Phase 8X — Event payment mode (architect-locked 2026-05-07).
/// Source of truth for free / paid / external-payment determination, replacing the
/// legacy <c>Event.IsFreeEvent</c> boolean. <c>IsFreeEvent</c> remains as a real entity
/// property kept in lockstep via <c>Event.SyncLegacyIsFree()</c> for one release per the
/// Phase 6A.86 single-source-of-truth discipline (no <c>builder.Ignore</c>, no shadow
/// property — Phase 6A.123 lesson).
///
/// Stored as <c>smallint</c> with DB-level <c>DEFAULT 0</c> (Phase 6A.123 lesson — never
/// rely on app-side defaults for NOT NULL columns) so legacy rows materialise as
/// <see cref="Free"/> automatically. Backfill in Phase 8X.2 migration sets paid rows to
/// <see cref="OnPlatformPaid"/>.
///
/// Compatibility (architect-locked):
/// - <see cref="ExternalPaid"/> forces <c>RegistrationMode = NoRegistration</c> (Mode C).
/// - <see cref="ExternalPaid"/> blocks <c>AssignedSeating</c>, add-ons, waitlist, check-in QR.
/// - <see cref="ExternalPaid"/> allows signup lists, donations, sponsors; ticket tiers display-only.
/// </summary>
public enum EventPaymentMode : short
{
    /// <summary>Free event — no pricing required, no payment collected.</summary>
    Free = 0,

    /// <summary>Paid event with payment collected on LankaConnect via Stripe.</summary>
    OnPlatformPaid = 1,

    /// <summary>Paid event whose payment + registration happens off-platform. Pricing
    /// is displayed; in-page CTA links to an external URL with optional vendor name +
    /// instructions. No internal Registration row is created.</summary>
    ExternalPaid = 2,
}
