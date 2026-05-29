using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.Entities;

/// <summary>
/// Phase 6A.154: a retired vanity URL slug. When an organizer changes or
/// clears <see cref="Event.VanitySlug"/>, the old value is preserved here so
/// previously-shared share links keep working (permanent 301 redirect to the
/// event's current canonical URL).
///
/// Append-only by design — aliases are never deleted, never reused by another
/// event. The partial unique index spans the alias namespace + the active-slug
/// namespace so collisions are impossible at the DB level.
/// </summary>
public class EventSlugAlias : BaseEntity
{
    /// <summary>
    /// The event this alias belongs to. Cascade-deletes with the event row.
    /// </summary>
    public Guid EventId { get; private set; }

    /// <summary>
    /// The retired slug value. Same shape rules as <see cref="ValueObjects.EventVanitySlug"/>
    /// (lowercase ASCII, 3–80 chars). Stored as raw string here because the
    /// VO has already validated the value at the moment of demotion;
    /// re-running validation on every read is wasted work.
    /// </summary>
    public string Alias { get; private set; } = string.Empty;

    /// <summary>
    /// When the alias was first registered as a vanity slug on the event.
    /// Used purely for organizer-side audit / "show me when each old URL
    /// was active". Cheap to populate; useful for support tickets.
    /// </summary>
    public DateTime ActivatedAt { get; private set; }

    /// <summary>
    /// When the alias was demoted (slug was changed/cleared). Equals
    /// <see cref="BaseEntity.CreatedAt"/> for the alias row.
    /// </summary>
    public DateTime RetiredAt { get; private set; }

    // EF Core ctor.
    private EventSlugAlias() { }

    private EventSlugAlias(Guid eventId, string alias, DateTime activatedAt, DateTime retiredAt)
    {
        EventId = eventId;
        Alias = alias;
        ActivatedAt = activatedAt;
        RetiredAt = retiredAt;
    }

    /// <summary>
    /// Factory used by the Event aggregate when a slug is changed or cleared.
    /// Not public-static-Result because the VO already validated the value
    /// upstream; we trust the caller. Internal-by-convention via the
    /// aggregate-only public API surface (only <c>Event.SetVanitySlug</c>
    /// constructs these).
    /// </summary>
    internal static EventSlugAlias Create(Guid eventId, string alias, DateTime activatedAt, DateTime retiredAt)
    {
        return new EventSlugAlias(eventId, alias, activatedAt, retiredAt);
    }
}
