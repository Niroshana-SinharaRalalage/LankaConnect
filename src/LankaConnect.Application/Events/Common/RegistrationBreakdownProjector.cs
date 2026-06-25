using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users.DomainEvents; // W4.7.a: user-aggregate events moved here
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace LankaConnect.Application.Events.Common;

/// <summary>
/// Phase 7F-E.2 (architect-approved 2026-05-01): shared post-projection helper that
/// loads the registration's mode + lead name + head-count + attendees in one
/// lightweight query and builds a <see cref="RegistrationBreakdown"/> via the formatter.
///
/// Used by every query handler that returns <see cref="RegistrationDetailsDto"/> so all
/// FE consumers see the same breakdown shape regardless of which endpoint built the DTO.
///
/// Design choice: secondary lightweight query rather than refactoring the existing EF
/// projections in <c>GetRegistrationByIdQueryHandler</c> and
/// <c>GetUserRegistrationForEventQueryHandler</c>. The projections are intricate (handle
/// many edge cases including bundled financial items) — a secondary load keeps the
/// existing code path untouched and limits regression risk to 7F-E.2's scope. Two-query
/// cost is negligible for single-row registration lookups.
/// </summary>
public static class RegistrationBreakdownProjector
{
    public sealed record BreakdownProjection(
        RegistrationMode Mode,
        string? LeadAttendeeName,
        RegistrationBreakdown? Breakdown);

    public static async Task<BreakdownProjection> LoadAsync(
        IApplicationDbContext context, Guid registrationId, CancellationToken ct = default)
    {
        // Load the full Registration entity. Using a Select projection with
        // `r.Attendees.ToList()` doesn't translate cleanly because Attendees is mapped
        // via OwnsMany.ToJson() — EF Core can't project owned-JSON collections into an
        // anonymous-type projection here. Loading the entity and reading properties
        // in-memory is simpler and avoids the translation pitfall.
        var entity = await context.Registrations
            .Where(r => r.Id == registrationId)
            .FirstOrDefaultAsync(ct);

        if (entity == null)
            return new BreakdownProjection(RegistrationMode.DetailedAttendees, null, null);

        RegistrationBreakdown? breakdown = null;
        if (entity.HeadCount != null)
        {
            breakdown = RegistrationBreakdownFormatter.FromHeadCount(entity.HeadCount, entity.RegistrationMode);
        }
        else if (entity.Attendees.Count > 0)
        {
            breakdown = RegistrationBreakdownFormatter.FromAttendees(entity.Attendees);
        }
        // else: registration has neither (defensive — leave Breakdown null)

        return new BreakdownProjection(entity.RegistrationMode, entity.LeadAttendeeName, breakdown);
    }
}
