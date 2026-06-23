namespace LankaConnect.Modules.Payments.Contracts;

/// <summary>
/// Cross-module mirror of <c>LankaConnect.Domain.Events.Enums.RefundRequestStatus</c>.
/// Deliberately duplicated at the Contracts boundary per the Forms / Communications
/// precedent - Contracts must not pull <c>LankaConnect.Domain</c>. Mapping is 1:1 on the
/// underlying byte value, so the projection is a pure cast at the
/// <c>Payments.Application.PaymentContractMappings</c> seam (added in 4.4.b).
/// </summary>
/// <remarks>
/// Wave 4.4.a (2026-06-23). State machine:
/// Pending -> Approved -> Processing -> Completed | Rejected | Withdrawn.
/// Once Approved, no backward transitions are allowed - money is moving.
/// </remarks>
public enum RefundRequestStatusDto : byte
{
    Pending = 0,
    Approved = 1,
    Processing = 2,
    Completed = 3,
    Rejected = 4,
    Withdrawn = 5,
}
