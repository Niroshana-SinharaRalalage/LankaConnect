namespace LankaConnect.Domain.Communications.Entities;

/// <summary>
/// Junction CLR entity for the Newsletter -> EmailGroup many-to-many relationship.
/// Holds only the foreign-key Guids — NO CLR navigation to the EmailGroup
/// aggregate root. Mirrors the
/// <see cref="LankaConnect.Products.LankaEvents.Domain.Entities.EventEmailGroupLink"/>
/// pattern; both share the same rationale (EF Core 8 cannot model a typed
/// M2M nav once the linked aggregate moves to a different module, and
/// the prior Wave 4.1.2 + 5.4 attempts proved this).
/// </summary>
/// <remarks>
/// Wave 5.4.d.1b (2026-06-22) — added to unblock the W5.4.d.2 physical
/// move of EmailGroup into Communications.Domain. Persisted to the
/// EXISTING <c>communications.newsletter_email_groups</c> table with the
/// same column shape (newsletter_id, email_group_id, created_at) and
/// composite PK. EF-snapshot-only change — no schema delta beyond the
/// single FK constraint drop.
/// <para>
/// The factory is <c>internal</c> so only the <see cref="Newsletter"/>
/// aggregate can mint links — this keeps the M2M lifecycle inside the
/// Newsletter aggregate's invariants.
/// </para>
/// </remarks>
public class NewsletterEmailGroupLink
{
    public Guid NewsletterId { get; private set; }
    public Guid EmailGroupId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // EF Core materialization ctor.
    private NewsletterEmailGroupLink() { }

    internal static NewsletterEmailGroupLink Create(Guid newsletterId, Guid emailGroupId) =>
        new()
        {
            NewsletterId = newsletterId,
            EmailGroupId = emailGroupId,
            CreatedAt = DateTime.UtcNow,
        };
}
