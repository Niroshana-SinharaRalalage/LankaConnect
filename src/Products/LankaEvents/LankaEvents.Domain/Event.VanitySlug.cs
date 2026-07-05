using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
namespace LankaConnect.Products.LankaEvents.Domain;

/// <summary>
/// Phase 6A.154: organizer-controlled vanity slug — the domain mutator.
///
/// Lets an organizer set <c>lankaconnect.app/cleveland-show</c> as a public
/// URL for their event. The VO (<see cref="EventVanitySlug"/>) owns the shape
/// validation; this partial owns the **aggregate-level invariants**:
///
/// <list type="bullet">
///   <item><description>Status lockout (architect D6 — same as 6A.153).</description></item>
///   <item><description>Alias bookkeeping — when slug changes/clears, the old
///         value is appended to <see cref="Event.SlugAliases"/> as a
///         permanent 301 source.</description></item>
/// </list>
///
/// Uniqueness across events is enforced at the DB level (partial unique
/// index on <c>vanity_slug</c> + alias namespace) and re-checked in the
/// command handler. The mutator does NOT call out to the repository —
/// aggregates are uniqueness-blind on purpose (no DB awareness in domain).
/// </summary>
public partial class Event
{
    /// <summary>
    /// Sets, replaces, or clears the vanity slug.
    /// </summary>
    /// <param name="newSlug">The new slug, or <c>null</c> to clear.</param>
    /// <returns>
    /// Success when the change is consistent with the event's status. Failure
    /// when status-locked. Same shape-validation errors flow up via the
    /// VO factory — callers are expected to construct the VO before calling
    /// this mutator (matches the existing pattern: <c>Event.Cancel</c>
    /// takes a pre-validated reason, doesn't re-validate).
    /// </returns>
    public Result SetVanitySlug(EventVanitySlug? newSlug)
    {
        // D6: status lockout. Same list as 6A.153 SetRegistrationWindow —
        // consistent rules across organizer-editable fields reduce the
        // surface area of "why can't I edit this?" support tickets.
        if (Status == EventStatus.Cancelled
            || Status == EventStatus.Completed
            || Status == EventStatus.Archived
            || Status == EventStatus.Active)
        {
            return Result.Failure(
                $"Vanity URL cannot be changed when event is {Status}.");
        }

        // No-op if nothing changed. Equality via VO's value-equality.
        // Avoids accidentally appending a meaningless alias row when an
        // organizer saves the edit form without touching the slug field.
        if (VanitySlug == newSlug)
        {
            return Result.Success();
        }

        // D3 + D16: if there WAS a slug and we're changing/clearing it,
        // the old value goes into the alias table. Aliases preserve share
        // links — anyone who pasted the old URL into Facebook months ago
        // still lands on the right event.
        if (VanitySlug != null)
        {
            // ActivatedAt = CreatedAt of the alias row's "active period
            // start" = best-effort the event's UpdatedAt or CreatedAt (we
            // don't currently track when a slug was first set — that's a
            // follow-up if we ever need precise activation history).
            // RetiredAt = now. Audit purposes only; not used for routing.
            var activatedAt = UpdatedAt ?? CreatedAt;
            _slugAliases.Add(EventSlugAlias.Create(
                eventId: Id,
                alias: VanitySlug.Value,
                activatedAt: activatedAt,
                retiredAt: DateTime.UtcNow));
        }

        VanitySlug = newSlug;

        return Result.Success();
    }
}
