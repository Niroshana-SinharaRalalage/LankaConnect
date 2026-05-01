using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events;
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
        var data = await context.Registrations
            .Where(r => r.Id == registrationId)
            .Select(r => new
            {
                r.RegistrationMode,
                r.LeadAttendeeName,
                r.HeadCount,
                Attendees = r.Attendees.ToList(),
            })
            .FirstOrDefaultAsync(ct);

        if (data == null)
            return new BreakdownProjection(RegistrationMode.DetailedAttendees, null, null);

        RegistrationBreakdown? breakdown = null;
        try
        {
            if (data.HeadCount != null)
            {
                breakdown = RegistrationBreakdownFormatter.FromHeadCount(data.HeadCount, data.RegistrationMode);
            }
            else if (data.Attendees.Count > 0)
            {
                breakdown = RegistrationBreakdownFormatter.FromAttendees(data.Attendees);
            }
            // else: registration has neither (defensive — leave Breakdown null)
        }
        catch (Exception)
        {
            // Defensive: never block the query response on a formatter exception. Logging
            // happens at the caller (handler) level; we just degrade to null breakdown so
            // the FE shows the legacy fallback rather than 500'ing.
            breakdown = null;
        }

        return new BreakdownProjection(data.RegistrationMode, data.LeadAttendeeName, breakdown);
    }
}
