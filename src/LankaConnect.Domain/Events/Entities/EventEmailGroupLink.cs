namespace LankaConnect.Domain.Events.Entities;

/// <summary>
/// Junction CLR entity for the Event -> EmailGroup many-to-many relationship.
/// Holds only the foreign-key Guids — NO CLR navigation to the EmailGroup
/// aggregate root. This is the architect-prescribed shape that replaces the
/// Wave 5.3-pre `_emailGroupEntities: List&lt;EmailGroup&gt;` typed nav (which
/// blocked the prior Wave 4.1.2 attempt because EF Core 8 does not support
/// pure shadow collection navs without a CLR property).
/// </summary>
/// <remarks>
/// Wave 5.4.c.0 (2026-06-13). Persisted to the EXISTING <c>event_email_groups</c>
/// table with the same column shape (event_id, email_group_id, assigned_at) and
/// composite PK. The change is EF-snapshot-only — no schema delta — so the
/// rebaseline migration ships with empty <c>Up()</c>/<c>Down()</c> bodies per
/// the memory pin <c>feedback_empty_up_snapshot_rebaseline</c>.
/// <para>
/// The factory is <c>internal</c> so only the <see cref="Event"/> aggregate
/// can mint links — this keeps the M2M lifecycle inside the Event aggregate's
/// invariants and prevents callers from constructing detached link rows.
/// </para>
/// </remarks>
public class EventEmailGroupLink
{
    public Guid EventId { get; private set; }
    public Guid EmailGroupId { get; private set; }
    public DateTime AssignedAt { get; private set; }

    // EF Core materialization ctor.
    private EventEmailGroupLink() { }

    internal static EventEmailGroupLink Create(Guid eventId, Guid emailGroupId) =>
        new()
        {
            EventId = eventId,
            EmailGroupId = emailGroupId,
            AssignedAt = DateTime.UtcNow,
        };
}
