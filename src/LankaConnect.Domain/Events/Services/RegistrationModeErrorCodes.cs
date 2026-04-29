namespace LankaConnect.Domain.Events.Services;

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
    /// Removal: when 7E.3b lands, drop the gate in
    /// <see cref="RegistrationModeCompatibility"/> and the corresponding mapper branch in
    /// <c>EventMappingProfile</c>. This constant can stay for one release as a no-op for
    /// caller back-compat, then be removed.
    /// </summary>
    public const string PaidHeadCountDeferred = "PaidHeadCountDeferred";
}
