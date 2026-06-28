namespace LankaConnect.Products.LankaEvents.Domain.Services;

/// <summary>
/// Stable error-code constants emitted by <see cref="RegistrationModeCompatibility"/>.
///
/// Why this exists (architect-required, Phase 7E paid-B-mode gate review iteration 1, edit #2):
/// Frontend code that needs to react to a specific compatibility failure (e.g. show a
/// "coming soon" panel instead of a fillable form) must not pattern-match on the human
/// copy embedded in <see cref="LankaConnect.Domain.Common.Result"/> failure messages —
/// that copy will get reworded over time and silently break the UI gate. Constants here
/// are the contract.
/// </summary>
public static class RegistrationModeErrorCodes
{
    /// <summary>
    /// Emitted when a head-count (Mode B) registration is requested on a PAID event before
    /// Phase 7E.3b ships the paid-B-mode + Stripe checkout flow.
    ///
    /// 2026-04-29 update: Phase 7E.3b shipped paid B-mode + Stripe checkout, so this gate
    /// is LIFTED in <see cref="RegistrationModeCompatibility"/>. The constant remains as a
    /// no-op for one release (caller back-compat), then can be removed.
    /// </summary>
    public const string PaidHeadCountDeferred = "PaidHeadCountDeferred";

    /// <summary>
    /// Emitted when a head-count (Mode B) registration is requested on a PAID event with a
    /// non-empty TierCounts axis before Phase 7E.3c ships the paid-B-mode + tier-counts
    /// pricing path.
    ///
    /// Why this exists (architect review iteration 1, edit #3): Phase 7E.3b lifted the
    /// blanket paid-B-mode gate but only ships single-price + dual-price (Adult/Child)
    /// paths. TierCounts pricing (e.g. "VIP × 2 + General × 3") is still being built. Without
    /// this guard, lifting the 7E.3b gate would expose paid B + tier counts with no pricing
    /// path → undefined behaviour / wrong charges.
    ///
    /// Removal: when 7E.3c lands, drop the gate in <c>Event.RegisterWithHeadCount</c> (or
    /// the pricing helper) and add the tier-counts pricing branch. This constant can stay
    /// for one release as a no-op, then be removed.
    /// </summary>
    public const string PaidHeadCountTiersDeferred = "PaidHeadCountTiersDeferred";
}
