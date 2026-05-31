using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

/// <summary>
/// Phase 6A.157 — domain event raised when a packaged sponsorship payment is
/// completed via Stripe webhook (or instantly for free packages).
///
/// Sibling event to <see cref="SponsorPaymentCompletedEvent"/>. The split is
/// intentional: package sponsors get a different confirmation email
/// (template-package-sponsor-confirmation with perks list, tier badge, and
/// included-ticket informational copy) than generic money sponsors
/// (template-sponsor-confirmation with the original "Thank you for your $X"
/// voice). Subscribers MUST subscribe to the specific event they care about
/// rather than null-checking package fields on the generic event.
///
/// Triggers:
/// - PackageSponsorCompletedEventHandler → sends template-package-sponsor-confirmation
/// - Future tier-grouping subscribers (Phase 6A.160) can also subscribe here.
///
/// Forward-compat note: the original 6A.156 design reserved 6A.158 for
/// auto-ticket-issuance from <see cref="IncludedTicketCountSnapshot"/>. Per
/// user direction 2026-05-31, 6A.158 is CANCELLED — the included-ticket
/// count is purely informational. The organizer admits sponsors at the
/// gate off-platform. This event still carries the count so subscribers
/// (e.g. the email handler) can include it in the buyer-facing copy.
/// </summary>
public record PackageSponsorCompletedEvent(
    Guid EventId,
    Guid SponsorId,
    Guid? SponsorUserId,
    string SponsorName,
    string SponsorEmail,
    string? SponsorOrganization,
    string PaymentIntentId,
    decimal Amount,
    string Currency,
    DateTime PaymentCompletedAt,
    // Package snapshot fields (always populated for package sponsors)
    Guid SponsorshipPackageId,
    string PackageNameSnapshot,
    string? PackageTierSnapshot,
    int IncludedTicketCountSnapshot
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
