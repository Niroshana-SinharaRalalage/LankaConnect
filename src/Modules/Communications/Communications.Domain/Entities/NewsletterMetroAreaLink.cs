namespace LankaConnect.Modules.Communications.Domain.Entities;

/// <summary>
/// Junction CLR entity for the Newsletter -> MetroArea many-to-many relationship.
/// Holds only the foreign-key Guids — NO CLR navigation to the MetroArea
/// aggregate root. Mirrors the
/// <see cref="NewsletterEmailGroupLink"/> pattern; same rationale (EF Core 8
/// cannot model a typed M2M nav once the linked aggregate moves to a different
/// assembly + invariant generic List&lt;T&gt; cannot be retyped to List&lt;object&gt;
/// without breaking EF's shadow-nav reflection assignment).
/// </summary>
/// <remarks>
/// Wave 5.2.d-hotfix2 (2026-06-28) — added to unblock the W5.1 MetroArea
/// move to Products.LankaEvents.Domain. The prior W5.1 attempt retyped
/// <c>private List&lt;MetroArea&gt; _metroAreaEntities</c> to
/// <c>List&lt;object&gt;</c> to avoid a LankaConnect.Domain --&gt; Products
/// dep cycle, but EF Core 8's shadow-nav hydrator assigns the strongly-typed
/// <c>List&lt;MetroArea&gt;</c> via reflection and the invariant generic cast
/// fails with <c>InvalidCastException</c> on every newsletter create. Founder
/// caught this 2026-06-28 during UI sweep: <c>POST /api/newsletters</c>
/// returned 500.
///
/// Persisted to the EXISTING <c>communications.newsletter_metro_areas</c>
/// table with the same column shape (newsletter_id, metro_area_id, created_at)
/// and composite PK. EF-snapshot-only change — no schema delta.
///
/// The factory is <c>internal</c> so only the <see cref="Newsletter"/>
/// aggregate can mint links — keeps the M2M lifecycle inside the
/// Newsletter aggregate's invariants.
/// </remarks>
public class NewsletterMetroAreaLink
{
    public Guid NewsletterId { get; private set; }
    public Guid MetroAreaId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // EF Core materialization ctor.
    private NewsletterMetroAreaLink() { }

    internal static NewsletterMetroAreaLink Create(Guid newsletterId, Guid metroAreaId) =>
        new()
        {
            NewsletterId = newsletterId,
            MetroAreaId = metroAreaId,
            CreatedAt = DateTime.UtcNow,
        };
}
